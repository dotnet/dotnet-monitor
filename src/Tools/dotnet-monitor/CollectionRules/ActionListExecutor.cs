// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions
{
    internal sealed class ActionListExecutor
    {
        ILogger<ActionListExecutor> _logger;
        ICollectionRuleActionOperations _actionOperations;

        public ActionListExecutor(ILogger<ActionListExecutor> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _actionOperations = serviceProvider.GetService<ICollectionRuleActionOperations>();
        }

        public async Task<List<CollectionRuleActionResult>> ExecuteActions(List<CollectionRuleActionOptions> collectionRuleActionOptions, IEndpointInfo endpointInfo, CancellationToken cancellationToken)
        {
            List<CollectionRuleActionResult> actionResults = new List<CollectionRuleActionResult>();

            int actionIndex = 0;

            foreach (CollectionRuleActionOptions actionOption in collectionRuleActionOptions)
            {
                // TODO: Not currently accounting for properties from previous executed actions

                _logger.LogInformation($"Action {actionIndex}: {actionOption.Type}");

                try
                {
                    ICollectionRuleActionProxy action;
                    _actionOperations.TryCreateAction(actionOption.Type, out action);

                    CollectionRuleActionResult result = await action.ExecuteAsync(actionOption.Settings, endpointInfo, cancellationToken);
                    actionResults.Add(result);
                }
                catch (CollectionRuleActionException e)
                {
                    throw new CollectionRuleActionExecutionException(e.Message, actionIndex);
                }

                _logger.LogInformation($"Action {actionIndex}: Completed");

                ++actionIndex;
            }

            return actionResults;
        }
    }

    internal class CollectionRuleActionExecutionException : Exception
    {
        public readonly int ActionIndex;

        public CollectionRuleActionExecutionException(string message, int actionIndex) : base(message)
        {
            ActionIndex = actionIndex;
        }
    }
}