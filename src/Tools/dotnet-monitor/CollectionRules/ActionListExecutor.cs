// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Exceptions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Extensions.Logging;
using System;
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
            CancellationToken cancellationToken)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            int actionIndex = 0;

            foreach (CollectionRuleActionOptions actionOption in context.Options.Actions)
            {
                // TODO: Not currently accounting for properties from previous executed actions

                KeyValueLogScope actionScope = new();
                actionScope.AddCollectionRuleAction(actionOption.Type, actionIndex);
                using IDisposable actionScopeRegistration = _logger.BeginScope(actionScope);

                _logger.CollectionRuleActionStarted(context.Name, actionOption.Type);

                try
                {
                    ICollectionRuleActionProxy action;

                    if (!_actionOperations.TryCreateAction(actionOption.Type, out action))
                    {
                        throw new InvalidOperationException(Strings.ErrorMessage_CouldNotMapToAction);
                    }

                    await action.ExecuteAsync(actionOption.Settings, context.ProcessInfo, cancellationToken);
                }
                catch (Exception ex) when (ShouldHandleException(ex, context.Name, actionOption.Type))
                {
                    throw new CollectionRuleActionExecutionException(ex, actionOption.Type, actionIndex);
                }

                _logger.CollectionRuleActionCompleted(context.Name, actionOption.Type);

                ++actionIndex;
            }
        }

        private bool ShouldHandleException(Exception ex, string ruleName, string actionType)
        {
            _logger.CollectionRuleActionFailed(ruleName, actionType, ex);

            return ex is not OperationCanceledException;
        }
    }
}