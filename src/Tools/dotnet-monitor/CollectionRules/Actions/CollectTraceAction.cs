// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Diagnostics.Monitoring.WebApi.Validation;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Utils = Microsoft.Diagnostics.Monitoring.WebApi.Utilities;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions
{
    internal sealed class CollectTraceAction : ICollectionRuleAction<CollectTraceOptions>
    {
        //private readonly IDumpService _dumpService;
        private readonly IServiceProvider _serviceProvider;

        public CollectTraceAction(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            //_dumpService = serviceProvider.GetRequiredService<IDumpService>();
        }

        public async Task<CollectionRuleActionResult> ExecuteAsync(CollectTraceOptions options, IEndpointInfo endpointInfo, CancellationToken token)
        {
            TimeSpan duration = options.Duration.GetValueOrDefault(TimeSpan.Parse(CollectTraceOptionsDefaults.Duration));
            string egressProvider = options.Egress;

            string traceFilePath = string.Empty;

            if (options.Profile != null && options.Providers == null)
            {
                TraceProfile profile = options.Profile.Value;
                int metricsIntervalSeconds = options.MetricsIntervalSeconds.GetValueOrDefault(CollectTraceOptionsDefaults.MetricsIntervalSeconds);

                // "Standard"

                var configurations = new List<MonitoringSourceConfiguration>();
                if (profile.HasFlag(TraceProfile.Cpu))
                {
                    configurations.Add(new CpuProfileConfiguration());
                }
                if (profile.HasFlag(TraceProfile.Http))
                {
                    configurations.Add(new HttpRequestSourceConfiguration());
                }
                if (profile.HasFlag(TraceProfile.Logs))
                {
                    configurations.Add(new LoggingSourceConfiguration(
                        LogLevel.Trace,
                        LogMessageType.FormattedMessage | LogMessageType.JsonMessage,
                        filterSpecs: null,
                        useAppFilters: true));
                }
                if (profile.HasFlag(TraceProfile.Metrics))
                {
                    configurations.Add(new MetricSourceConfiguration(metricsIntervalSeconds, Enumerable.Empty<string>()));
                }

                var aggregateConfiguration = new AggregateSourceConfiguration(configurations.ToArray());

                traceFilePath = await StartTrace(endpointInfo, aggregateConfiguration, duration, egressProvider);
            }
            else if (options.Providers != null && options.Profile == null)
            {
                List<Monitoring.WebApi.Models.EventPipeProvider> optionsProviders = options.Providers;
                bool requestRundown = options.RequestRundown.GetValueOrDefault(CollectTraceOptionsDefaults.RequestRundown);
                int bufferSizeMegabytes = options.BufferSizeMegabytes.GetValueOrDefault(CollectTraceOptionsDefaults.BufferSizeMegabytes);

                // "Custom"

                var providers = new List<NETCore.Client.EventPipeProvider>();

                foreach (Monitoring.WebApi.Models.EventPipeProvider providerModel in optionsProviders)
                {
                    // Probably should spike IntegerOrHexStringAttribute into a Utilities method (need Dump to merge for that)
                    if (!IntegerOrHexStringAttribute.TryParse(providerModel.Keywords, out long keywords, out string parseError))
                    {
                        throw new InvalidOperationException(parseError);
                    }

                    providers.Add(new Microsoft.Diagnostics.NETCore.Client.EventPipeProvider(
                        providerModel.Name,
                        providerModel.EventLevel,
                        keywords,
                        providerModel.Arguments
                        ));
                }

                var traceConfiguration = new EventPipeProviderSourceConfiguration(
                    providers: providers.ToArray(),
                    requestRundown: requestRundown,
                    bufferSizeInMB: bufferSizeMegabytes);

                traceFilePath = await StartTrace(endpointInfo, traceConfiguration, duration, egressProvider);

            }
            else
            {
                throw new ArgumentException("One of the Profile and Providers fields must be provided."); // Temporary message -> would be put into Strings.resx if we keep the validation
            }

            return new CollectionRuleActionResult()
            {
                OutputValues = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    { "EgressPath", traceFilePath }
                }
            };
        }

        private async Task<string> StartTrace(
            IEndpointInfo endpointInfo,
            MonitoringSourceConfiguration configuration,
            TimeSpan duration,
            string egressProvider)
        {
            string fileName = FormattableString.Invariant($"{Utils.GetFileNameTimeStampUtcNow()}_{endpointInfo.ProcessId}.nettrace");

            Func<Stream, CancellationToken, Task> action = async (outputStream, token) =>
            {
                Func<Stream, CancellationToken, Task> streamAvailable = async (Stream eventStream, CancellationToken token) =>
                {
                    //Buffer size matches FileStreamResult
                    //CONSIDER Should we allow client to change the buffer size?
                    await eventStream.CopyToAsync(outputStream, 0x10000, token);
                };

                var client = new DiagnosticsClient(endpointInfo.Endpoint);

                await using EventTracePipeline pipeProcessor = new EventTracePipeline(client, new EventTracePipelineSettings
                {
                    Configuration = configuration,
                    Duration = duration,
                }, streamAvailable);

                await pipeProcessor.RunAsync(token);
            };

            KeyValueLogScope scope = Utils.GetScope(Utils.ArtifactType_Trace, endpointInfo);

            EgressOperation egressOperation = new EgressOperation(
                action,
                egressProvider,
                fileName,
                endpointInfo,
                ContentTypes.ApplicationOctetStream,
                scope);

            using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(CommonTestTimeouts.HttpApi);

            ExecutionResult<EgressResult> result = await egressOperation.ExecuteAsync(_serviceProvider, cancellationTokenSource.Token);

            string traceFilePath = result.Result.Value;

            return traceFilePath;
        }
    }
}