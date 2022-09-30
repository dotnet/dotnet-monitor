// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.WebApi.Validation;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal static class TraceUtilities
    {
        // Buffer size matches FileStreamResult
        private const int DefaultBufferSize = 0x10000;

        public static string GenerateTraceFileName(IEndpointInfo endpointInfo)
        {
            return FormattableString.Invariant($"{Utilities.GetFileNameTimeStampUtcNow()}_{endpointInfo.ProcessId}.nettrace");
        }

        public static MonitoringSourceConfiguration GetTraceConfiguration(Models.TraceProfile profile, float metricsIntervalSeconds)
        {
            var configurations = new List<MonitoringSourceConfiguration>();
            if (profile.HasFlag(Models.TraceProfile.Cpu))
            {
                configurations.Add(new CpuProfileConfiguration());
            }
            if (profile.HasFlag(Models.TraceProfile.Http))
            {
                configurations.Add(new HttpRequestSourceConfiguration());
            }
            if (profile.HasFlag(Models.TraceProfile.Logs))
            {
                configurations.Add(new LoggingSourceConfiguration(
                    LogLevel.Trace,
                    LogMessageType.FormattedMessage | LogMessageType.JsonMessage,
                    filterSpecs: null,
                    useAppFilters: true));
            }
            if (profile.HasFlag(Models.TraceProfile.Metrics))
            {
                configurations.Add(new MetricSourceConfiguration(metricsIntervalSeconds, Enumerable.Empty<string>()));
            }

            return new AggregateSourceConfiguration(configurations.ToArray());
        }

        public static MonitoringSourceConfiguration GetTraceConfiguration(Models.EventPipeProvider[] configurationProviders, bool requestRundown, int bufferSizeInMB)
        {
            var providers = new List<EventPipeProvider>();

            foreach (Models.EventPipeProvider providerModel in configurationProviders)
            {
                if (!IntegerOrHexStringAttribute.TryParse(providerModel.Keywords, out long keywords, out string parseError))
                {
                    throw new InvalidOperationException(parseError);
                }

                providers.Add(new EventPipeProvider(
                    providerModel.Name,
                    providerModel.EventLevel,
                    keywords,
                    providerModel.Arguments
                    ));
            }

            return new EventPipeProviderSourceConfiguration(
                providers: providers.ToArray(),
                requestRundown: requestRundown,
                bufferSizeInMB: bufferSizeInMB);
        }

        public static async Task CaptureTraceAsync(TaskCompletionSource<object> startCompletionSource, IEndpointInfo endpointInfo, MonitoringSourceConfiguration configuration, TimeSpan duration, Stream outputStream, CancellationToken token)
        {
            Func<Stream, CancellationToken, Task> streamAvailable = async (Stream eventStream, CancellationToken token) =>
            {
                if (null != startCompletionSource)
                {
                    startCompletionSource.TrySetResult(null);
                }
                //CONSIDER Should we allow client to change the buffer size?
                await eventStream.CopyToAsync(outputStream, DefaultBufferSize, token);
            };

            var client = new DiagnosticsClient(endpointInfo.Endpoint);

            await using EventTracePipeline pipeProcessor = new EventTracePipeline(client, new EventTracePipelineSettings
            {
                Configuration = configuration,
                Duration = duration,
            }, streamAvailable);

            await pipeProcessor.RunAsync(token);
        }

        public static async Task CaptureTraceUntilEventAsync(TaskCompletionSource<object> startCompletionSource, IEndpointInfo endpointInfo, MonitoringSourceConfiguration configuration, TimeSpan timeout, Stream outputStream, string providerName, string eventName, IDictionary<string, string> payloadFilter, ILogger logger, CancellationToken token)
        {
            DiagnosticsClient client = new(endpointInfo.Endpoint);
            TaskCompletionSource<object> stoppingEventHitSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

            using IDisposable registration = token.Register(
                () => stoppingEventHitSource.TrySetCanceled(token));

            await using EventTracePipeline pipeProcessor = new(client, new EventTracePipelineSettings
            {
                Configuration = configuration,
                Duration = timeout,
            },
            async (eventStream, token) =>
            {
                startCompletionSource?.TrySetResult(null);
                await using EventMonitoringPassthroughStream eventMonitoringStream = new(
                    providerName,
                    eventName,
                    payloadFilter,
                    onEvent: (traceEvent) =>
                    {
                        logger.StoppingTraceEventHit(traceEvent);
                        stoppingEventHitSource.TrySetResult(null);
                    },
                    onPayloadFilterMismatch: logger.StoppingTraceEventPayloadFilterMismatch,
                    eventStream,
                    outputStream,
                    DefaultBufferSize,
                    callOnEventOnlyOnce: true,
                    leaveDestinationStreamOpen: true /* We do not have ownership of the outputStream */);

                await eventMonitoringStream.ProcessAsync(token);
            });

            Task pipelineRunTask = pipeProcessor.RunAsync(token);
            await Task.WhenAny(pipelineRunTask, stoppingEventHitSource.Task).Unwrap();

            if (stoppingEventHitSource.Task.IsCompleted)
            {
                await pipeProcessor.StopAsync(token);
            }
        }
    }
}
