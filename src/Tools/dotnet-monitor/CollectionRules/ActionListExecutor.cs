// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
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

        public ActionListExecutor(
            ILogger<ActionListExecutor> logger)
        {
            _logger = logger;
        }

        public async Task<List<CollectionRuleActionResult>> ExecuteActions(List<CollectionRuleActionOptions> collectionRuleActionOptions, IEndpointInfo endpointInfo, CancellationToken cancellationToken)
        {
            List<CollectionRuleActionResult> actionResults = new List<CollectionRuleActionResult>();

            foreach (CollectionRuleActionOptions actionOption in collectionRuleActionOptions)
            {
                // Not currently accounting for properties from previous executed actions

                try
                {
                    CollectionRuleActionResult result = await IdentifyCollectionRuleAction(actionOption, endpointInfo, cancellationToken);
                    actionResults.Add(result);
                }
                catch (FileNotFoundException fileNotFoundException)
                {
                    _logger.LogError(fileNotFoundException.Message);

                    return actionResults;
                }
                catch (InvalidOperationException invalidOperationException)
                {
                    _logger.LogError(invalidOperationException.Message);

                    return actionResults;
                }
                catch (TaskCanceledException taskCanceledException)
                {
                    _logger.LogError(taskCanceledException.Message);

                    return actionResults;
                }
            }

            return actionResults;
        }

        private async Task<CollectionRuleActionResult> IdentifyCollectionRuleAction(CollectionRuleActionOptions actionOptions, IEndpointInfo endpointInfo, CancellationToken cancellationToken)
        {
            switch (actionOptions.Type)
            {
                case "Execute":
                    return await PerformExecuteAction(actionOptions, endpointInfo, cancellationToken);
                case "CollectDump":
                    return await PerformCollectDumpAction(actionOptions, endpointInfo, cancellationToken);
                case "CollectGCDump":
                    return await PerformCollectGCDumpAction(actionOptions, endpointInfo, cancellationToken);
                case "CollectLogs":
                    return await PerformCollectLogsAction(actionOptions, endpointInfo, cancellationToken);
                case "CollectTrace":
                    return await PerformCollectTraceAction(actionOptions, endpointInfo, cancellationToken);
                default:
                    throw new ArgumentException($"Invalid action type {actionOptions.Type}.");
            }
        }

        private async Task<CollectionRuleActionResult> PerformExecuteAction(CollectionRuleActionOptions actionOptions, IEndpointInfo endpointInfo, CancellationToken cancellationToken)
        {
            ExecuteAction action = new();
            ExecuteOptions options = (ExecuteOptions)(actionOptions.Settings);

            return await action.ExecuteAsync(options, endpointInfo, cancellationToken);
        }

        private async Task<CollectionRuleActionResult> PerformCollectDumpAction(CollectionRuleActionOptions actionOptions, IEndpointInfo endpointInfo, CancellationToken cancellationToken)
        {
            CollectDumpAction action = new();
            CollectDumpOptions options = (CollectDumpOptions)(actionOptions.Settings);

            return await action.ExecuteAsync(options, endpointInfo, cancellationToken);
        }

        private async Task<CollectionRuleActionResult> PerformCollectGCDumpAction(CollectionRuleActionOptions actionOptions, IEndpointInfo endpointInfo, CancellationToken cancellationToken)
        {
            CollectGCDumpAction action = new();
            CollectGCDumpOptions options = (CollectGCDumpOptions)(actionOptions.Settings);

            return await action.ExecuteAsync(options, endpointInfo, cancellationToken);
        }

        private async Task<CollectionRuleActionResult> PerformCollectLogsAction(CollectionRuleActionOptions actionOptions, IEndpointInfo endpointInfo, CancellationToken cancellationToken)
        {
            CollectLogsAction action = new();
            CollectLogsOptions options = (CollectLogsOptions)(actionOptions.Settings);

            return await action.ExecuteAsync(options, endpointInfo, cancellationToken);
        }

        private async Task<CollectionRuleActionResult> PerformCollectTraceAction(CollectionRuleActionOptions actionOptions, IEndpointInfo endpointInfo, CancellationToken cancellationToken)
        {
            CollectTraceAction action = new();
            CollectTraceOptions options = (CollectTraceOptions)(actionOptions.Settings);

            return await action.ExecuteAsync(options, endpointInfo, cancellationToken);
        }
    }
}