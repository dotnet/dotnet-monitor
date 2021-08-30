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
        private readonly ILogger<ActionListExecutor> _logger;
        private readonly ICollectionRuleActionOperations _actionOperations;

        public ActionListExecutor(ILogger<ActionListExecutor> logger, ICollectionRuleActionOperations actionOperations)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _actionOperations = actionOperations ?? throw new ArgumentNullException(nameof(actionOperations));
        }

        public async Task ExecuteActions(IEnumerable<CollectionRuleActionOptions> collectionRuleActionOptions, IEndpointInfo endpointInfo, CancellationToken cancellationToken)
        {
            if (collectionRuleActionOptions == null)
            {
                throw new ArgumentNullException(nameof(collectionRuleActionOptions));
            }

            int actionIndex = 0;

            foreach (CollectionRuleActionOptions actionOption in collectionRuleActionOptions)
            {
                // TODO: Not currently accounting for properties from previous executed actions

                _logger.LogInformation($"Action {actionIndex}: {actionOption.Type}");

                try
                {
                    ICollectionRuleActionProxy action;

                    if (!_actionOperations.TryCreateAction(actionOption.Type, out action))
                    {
                        throw new InvalidOperationException(Strings.ErrorMessage_CouldNotMapToAction);
                    }

                    await action.ExecuteAsync(actionOption.Settings, endpointInfo, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    throw new CollectionRuleActionExecutionException(ex, actionIndex);
                }

                _logger.LogInformation($"Action {actionIndex}: Completed");

                ++actionIndex;
            }
        }
    }
}