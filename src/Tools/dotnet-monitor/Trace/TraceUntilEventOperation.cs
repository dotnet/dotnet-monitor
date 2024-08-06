// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class TraceUntilEventOperation : AbstractTraceOperation
    {
        private readonly string _providerName;
        private readonly string _eventName;
        private readonly IDictionary<string, string>? _payloadFilter;

        private readonly TaskCompletionSource<object?> _eventStreamAvailableCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<object?> _stoppingEventHitSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TraceUntilEventOperation(
            IEndpointInfo endpointInfo,
            EventTracePipelineSettings settings,
            string providerName,
            string eventName,
            IDictionary<string, string>? payloadFilter,
            OperationTrackerService trackerService,
            ILogger logger)
            : base(endpointInfo, settings, trackerService, logger)
        {
            _providerName = providerName;
            _eventName = eventName;
            _payloadFilter = payloadFilter;
        }

        protected override EventTracePipeline CreatePipeline(Stream outputStream)
        {
            DiagnosticsClient client = new(EndpointInfo.Endpoint);
            return new EventTracePipeline(client, _settings,
                async (eventStream, token) =>
                {
                    _eventStreamAvailableCompletionSource.TrySetResult(null);

                    await using EventMonitor eventMonitor = new(
                        _providerName,
                        _eventName,
                        _payloadFilter,
                        onEvent: (traceEvent) =>
                        {
                            Logger.StoppingTraceEventHit(traceEvent);
                            _stoppingEventHitSource.TrySetResult(null);
                        },
                        onPayloadFilterMismatch: Logger.StoppingTraceEventPayloadFilterMismatch,
                        eventStream: new PassthroughStream(eventStream, outputStream, DefaultBufferSize, leaveDestinationStreamOpen: true /* We do not have ownership of the outputStream */),
                        callOnEventOnlyOnce: true);

                    await eventMonitor.ProcessAsync(token);
                });
        }

        protected override async Task<Task> StartPipelineAsync(EventTracePipeline pipeline, CancellationToken token)
        {
            Task pipelineRunTask = pipeline.RunAsync(token);
            await _eventStreamAvailableCompletionSource.Task.WaitAsync(token);

            return Task.Run(async () =>
            {
                await Task.WhenAny(pipelineRunTask, _stoppingEventHitSource.Task).Unwrap();

                if (_stoppingEventHitSource.Task.IsCompleted)
                {
                    await pipeline.StopAsync(token);
                    await pipelineRunTask;
                }
            }, token);
        }
    }
}
