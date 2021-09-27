// Licensed to the .NET Foundation under one or more agreements.
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

        public async Task ExecuteActions(
            CollectionRuleContext context,
            Action startCallback,
            CancellationToken cancellationToken)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            int actionIndex = 0;
            List<ActionCompletionEntry> deferredCompletions = new(context.Options.Actions.Count);

            try
            {
                // Start and optionally wait for each action to complete
                foreach (CollectionRuleActionOptions actionOption in context.Options.Actions)
                {
                    // TODO: Not currently accounting for properties from previous executed actions

                    KeyValueLogScope actionScope = new();
                    actionScope.AddCollectionRuleAction(actionOption.Type, actionIndex);
                    using IDisposable actionScopeRegistration = _logger.BeginScope(actionScope);

                    _logger.CollectionRuleActionStarted(context.Name, actionOption.Type);

                    try
                    {
                        ICollectionRuleActionFactoryProxy factory;

                        if (!_actionOperations.TryCreateFactory(actionOption.Type, out factory))
                        {
                            throw new InvalidOperationException(Strings.ErrorMessage_CouldNotMapToAction);
                        }

                        ICollectionRuleAction action = factory.Create(context.EndpointInfo, actionOption.Settings);

                        try
                        {
                            await action.StartAsync(cancellationToken);

                            // Check if the action completion should be awaited synchronously (in respected to
                            // starting the next action). If not, add a deferred entry so that it can be completed
                            // add the end of the execution list.
                            if (actionOption.WaitForCompletion.GetValueOrDefault(CollectionRuleActionOptionsDefaults.WaitForCompletion))
                            {
                                await action.WaitForCompletionAsync(cancellationToken);

                                _logger.CollectionRuleActionCompleted(context.Name, actionOption.Type);
                            }
                            else
                            {
                                deferredCompletions.Add(new(action, actionOption, actionIndex));

                                // Set action to null to skip disposal
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
                startCallback?.Invoke();

                // Wait for any actions whose completion has been deferred.
                for (int i = 0; i < deferredCompletions.Count; i++)
                {
                    ActionCompletionEntry deferredCompletion = deferredCompletions[i];
                    try
                    {
                        await deferredCompletion.Action.WaitForCompletionAsync(cancellationToken);

                        _logger.CollectionRuleActionCompleted(context.Name, deferredCompletion.Options.Type);
                    }
                    catch (Exception ex) when (ShouldHandleException(ex, context.Name, deferredCompletion.Options.Type))
                    {
                        throw new CollectionRuleActionExecutionException(ex, deferredCompletion.Options.Type, deferredCompletion.Index);
                    }
                    finally
                    {
                        await DisposableHelper.DisposeAsync(deferredCompletion.Action);

                        deferredCompletions.RemoveAt(i);
                        i--;
                    }
                }
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