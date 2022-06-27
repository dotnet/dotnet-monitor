// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Exceptions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Utils = Microsoft.Diagnostics.Monitoring.WebApi.Utilities;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions
{
    internal sealed class CollectLiveMetricsActionFactory :
        ICollectionRuleActionFactory<CollectLiveMetricsOptions>
    {
        private readonly IServiceProvider _serviceProvider;

        public CollectLiveMetricsActionFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public ICollectionRuleAction Create(IEndpointInfo endpointInfo, CollectLiveMetricsOptions options)
        {
            if (null == options)
            {
                throw new ArgumentNullException(nameof(options));
            }

            ValidationContext context = new(options, _serviceProvider, items: null);
            Validator.ValidateObject(options, context, validateAllProperties: true);

            return new CollectLiveMetricsAction(_serviceProvider, endpointInfo, options);
        }

        private sealed class CollectLiveMetricsAction :
            CollectionRuleActionBase<CollectLiveMetricsOptions>
        {
            private readonly IServiceProvider _serviceProvider;

            public CollectLiveMetricsAction(IServiceProvider serviceProvider, IEndpointInfo endpointInfo, CollectLiveMetricsOptions options)
                : base(endpointInfo, options)
            {
                _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            }

            protected override async Task<CollectionRuleActionResult> ExecuteCoreAsync(
                TaskCompletionSource<object> startCompletionSource,
                CancellationToken token)
            {
                TimeSpan duration = Options.Duration.GetValueOrDefault(TimeSpan.Parse(CollectLogsOptionsDefaults.Duration));
                string egressProvider = Options.Egress;


                /*
                 * 
                 * 
                return InvokeForProcess(async (processInfo) =>
                {
                    string fileName = GetMetricFilename(processInfo);

                    Func<Stream, CancellationToken, Task> action = async (outputStream, token) =>
                    {
                        var client = new DiagnosticsClient(processInfo.EndpointInfo.Endpoint);
                        EventPipeCounterPipelineSettings settings = EventCounterSettingsFactory.CreateSettings(
                            _counterOptions.CurrentValue,
                            includeDefaults: true,
                            durationSeconds: durationSeconds);

                        await using EventCounterPipeline eventCounterPipeline = new EventCounterPipeline(client,
                            settings,
                            loggers:
                            new[] { new JsonCounterLogger(outputStream) });

                        await eventCounterPipeline.RunAsync(token);
                    };

                    return await Result(Utilities.ArtifactType_Metrics,
                        egressProvider,
                        action,
                        fileName,
                        ContentTypes.ApplicationJsonSequence,
                        processInfo.EndpointInfo);
                }, processKey, Utilities.ArtifactType_Metrics);
                 */















                var settings = new EventLogsPipelineSettings()
                {
                    Duration = duration,
                    LogLevel = defaultLevel,
                    UseAppFilters = useAppFilters,
                    FilterSpecs = filterSpecs
                };

                string fileName = MetricsUtilities.GetMetricFilename(EndpointInfo);

                KeyValueLogScope scope = Utils.CreateArtifactScope(Utils.ArtifactType_Logs, EndpointInfo);

                EgressOperation egressOperation = new EgressOperation(
                    (outputStream, token) => MetricsUtilities.CaptureLiveMetricsAsync(startCompletionSource, EndpointInfo, outputStream, token),
                    egressProvider,
                    fileName,
                    EndpointInfo,
                    ContentTypes.ApplicationOctetStream,
                    scope);

                ExecutionResult<EgressResult> result = await egressOperation.ExecuteAsync(_serviceProvider, token);
                if (null != result.Exception)
                {
                    throw new CollectionRuleActionException(result.Exception);
                }
                string logsFilePath = result.Result.Value;

                return new CollectionRuleActionResult()
                {
                    OutputValues = new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        { CollectionRuleActionConstants.EgressPathOutputValueName, logsFilePath }
                    }
                };
            }
        }
    }
}
