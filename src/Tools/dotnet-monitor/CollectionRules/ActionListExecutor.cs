﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Exceptions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions
{
    internal sealed class ActionListExecutor
    {
        private readonly ILogger<CollectionRuleService> _logger;
        private readonly ICollectionRuleActionOperations _actionOperations;

        public ActionListExecutor(ILogger<CollectionRuleService> logger, ICollectionRuleActionOperations actionOperations)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _actionOperations = actionOperations ?? throw new ArgumentNullException(nameof(actionOperations));
        }

        //CONSIDER Only named rules currently return results since only named results can be referenced
        public async Task<IDictionary<string, CollectionRuleActionResult>> ExecuteActions(
            CollectionRuleContext context,
            Action startCallback,
            CancellationToken cancellationToken)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            bool started = false;
            Action wrappedStartCallback = () =>
            {
                if (!started)
                {
                    started = true;
                    startCallback?.Invoke();
                }
            };

            int actionIndex = 0;
            List<ActionCompletionEntry> deferredCompletions = new(context.Options.Actions.Count);

            var actionResults = new Dictionary<string, CollectionRuleActionResult>(StringComparer.Ordinal);
            var dependencyAnalyzer = ActionOptionsDependencyAnalyzer.Create(context);

            try
            {
                // Start and optionally wait for each action to complete
                foreach (CollectionRuleActionOptions actionOption in context.Options.Actions)
                {
                    KeyValueLogScope actionScope = new();
                    actionScope.AddCollectionRuleAction(actionOption.Type, actionIndex);
                    using IDisposable actionScopeRegistration = _logger.BeginScope(actionScope);

                    _logger.CollectionRuleActionStarted(context.Name, actionOption.Type);

                    try
                    {
                        IList<CollectionRuleActionOptions> actionDependencies = dependencyAnalyzer.GetActionDependencies(actionIndex);
                        foreach (CollectionRuleActionOptions actionDependency in actionDependencies)
                        {
                            for (int i = 0; i < deferredCompletions.Count; i++)
                            {
                                ActionCompletionEntry deferredCompletion = deferredCompletions[i];
                                if (string.Equals(deferredCompletion.Options.Name, actionDependency.Name, StringComparison.Ordinal))
                                {
                                    deferredCompletions.RemoveAt(i);
                                    i--;
                                    await WaitForCompletion(context, wrappedStartCallback, actionResults, deferredCompletion, cancellationToken);
                                    break;
                                }
                            }
                        }

                        ICollectionRuleActionFactoryProxy factory;

                        if (!_actionOperations.TryCreateFactory(actionOption.Type, out factory))
                        {
                            throw new InvalidOperationException(Strings.ErrorMessage_CouldNotMapToAction);
                        }

                        object newSettings = dependencyAnalyzer.SubstituteOptionValues(actionResults, actionIndex, actionOption.Settings);
                        ICollectionRuleAction action = factory.Create(context.EndpointInfo, newSettings);

                        try
                        {
                            CollectionRuleMetadata metadata = new()
                            {
                                CollectionRuleName = context.Name,
                                ActionListIndex = actionIndex,
                                ActionName = actionOption.Name
                            };

                            await action.StartAsync(metadata, cancellationToken);

                            // Check if the action completion should be awaited synchronously (in respect to
                            // starting the next action). If not, add a deferred entry so that it can be completed
                            // after starting each action in the list.
                            if (actionOption.WaitForCompletion.GetValueOrDefault(CollectionRuleActionOptionsDefaults.WaitForCompletion))
                            {
                                await WaitForCompletion(context, wrappedStartCallback, actionResults, action, actionOption, cancellationToken);
                            }
                            else
                            {
                                deferredCompletions.Add(new(action, actionOption, actionIndex));

                                // Set to null to skip disposal
                                action = null;
                            }
                        }
                        finally
                        {
                            await DisposableHelper.DisposeAsync(action);
                        }
                    }
                    catch (Exception ex) when (ShouldHandleException(ex, context.Name, actionOption.Type))
                    {
                        throw new CollectionRuleActionExecutionException(ex, actionOption.Type, actionIndex);
                    }

                    ++actionIndex;
                }

                // Notify that all actions have started
                wrappedStartCallback?.Invoke();

                // Wait for any actions whose completion has been deferred.
                while (deferredCompletions.Count > 0)
                {
                    ActionCompletionEntry deferredCompletion = deferredCompletions[0];
                    deferredCompletions.RemoveAt(0);
                    await WaitForCompletion(context, wrappedStartCallback, actionResults, deferredCompletion, cancellationToken);
                }

                return actionResults;
            }
            finally
            {
                // Always dispose any deferred action completions so that those actions
                // are stopped before leaving the action list executor.
                foreach (ActionCompletionEntry deferredCompletion in deferredCompletions)
                {
                    await DisposableHelper.DisposeAsync(deferredCompletion.Action);
                }
            }
        }

        private async Task WaitForCompletion(CollectionRuleContext context,
            Action startCallback,
            IDictionary<string, CollectionRuleActionResult> allResults,
            ICollectionRuleAction action,
            CollectionRuleActionOptions actionOption,
            CancellationToken cancellationToken)
        {
            //Before we wait for any other action to complete, we signal that execution has started.
            //This allows process resume to occur.
            startCallback?.Invoke();

            CollectionRuleActionResult results = await action.WaitForCompletionAsync(cancellationToken);
            if (!string.IsNullOrEmpty(actionOption.Name))
            {
                allResults.Add(actionOption.Name, results);
            }

            _logger.CollectionRuleActionCompleted(context.Name, actionOption.Type);
        }

        private async Task WaitForCompletion(CollectionRuleContext context,
            Action startCallback,
            IDictionary<string, CollectionRuleActionResult> allResults,
            ActionCompletionEntry entry,
            CancellationToken cancellationToken)
        {
            try
            {
                await WaitForCompletion(context, startCallback, allResults, entry.Action, entry.Options, cancellationToken);
            }
            catch (Exception ex) when (ShouldHandleException(ex, context.Name, entry.Options.Type))
            {
                throw new CollectionRuleActionExecutionException(ex, entry.Options.Type, entry.Index);
            }
            finally
            {
                await DisposableHelper.DisposeAsync(entry.Action);
            }
        }

        private bool ShouldHandleException(Exception ex, string ruleName, string actionType)
        {
            _logger.CollectionRuleActionFailed(ruleName, actionType, ex);

            return ex is not OperationCanceledException;
        }

        private sealed class ActionCompletionEntry
        {
            public ActionCompletionEntry(ICollectionRuleAction action, CollectionRuleActionOptions options, int index)
            {
                Action = action ?? throw new ArgumentNullException(nameof(action));
                Options = options ?? throw new ArgumentNullException(nameof(options));
                Index = index;
            }

            public ICollectionRuleAction Action { get; }

            public int Index { get; }

            public CollectionRuleActionOptions Options { get; }
        }
    }
}
