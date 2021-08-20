// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Configuration;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions
{
    internal sealed class ActionListExecutor
    {
        private readonly ICollectionRuleActionOptionsProvider _actionOptionsProvider;

        public ActionListExecutor(
            ICollectionRuleActionOptionsProvider actionOptionsProvider)
        {
            _actionOptionsProvider = actionOptionsProvider;
        }

        public async Task<List<CollectionRuleActionResult>> ExecuteActions(List<CollectionRuleActionOptions> collectionRuleActionOptions, IEndpointInfo endpointInfo, CancellationToken cancellationToken)
        {
            List<CollectionRuleActionResult> actionResults = new List<CollectionRuleActionResult>();

            foreach (CollectionRuleActionOptions actionOption in collectionRuleActionOptions)
            {
                CollectionRuleActionResult result = await IdentifyCollectionRuleAction(actionOption, endpointInfo, cancellationToken);
                actionResults.Add(result);
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
            ExecuteAction action = new();
            ExecuteOptions options = (ExecuteOptions)(actionOptions.Settings);

            return await action.ExecuteAsync(options, endpointInfo, cancellationToken);
        }

        private async Task<CollectionRuleActionResult> PerformCollectGCDumpAction(CollectionRuleActionOptions actionOptions, IEndpointInfo endpointInfo, CancellationToken cancellationToken)
        {
            ExecuteAction action = new();
            ExecuteOptions options = (ExecuteOptions)(actionOptions.Settings);

            return await action.ExecuteAsync(options, endpointInfo, cancellationToken);
        }

        private async Task<CollectionRuleActionResult> PerformCollectLogsAction(CollectionRuleActionOptions actionOptions, IEndpointInfo endpointInfo, CancellationToken cancellationToken)
        {
            ExecuteAction action = new();
            ExecuteOptions options = (ExecuteOptions)(actionOptions.Settings);

            return await action.ExecuteAsync(options, endpointInfo, cancellationToken);
        }

        private async Task<CollectionRuleActionResult> PerformCollectTraceAction(CollectionRuleActionOptions actionOptions, IEndpointInfo endpointInfo, CancellationToken cancellationToken)
        {
            ExecuteAction action = new();
            ExecuteOptions options = (ExecuteOptions)(actionOptions.Settings);

            return await action.ExecuteAsync(options, endpointInfo, cancellationToken);
        }
    }
}