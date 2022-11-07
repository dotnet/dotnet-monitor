// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class TraceUntilEventOperation : AbstractTraceOperation
    {
        private readonly TraceEventFilter _stoppingEvent;

        private readonly TaskCompletionSource<object> _eventStreamAvailableCompletionSource = new();
        private readonly TaskCompletionSource<object> _stoppingEventHitSource = new();

        public TraceUntilEventOperation(IEndpointInfo endpointInfo, EventTracePipelineSettings settings, TraceEventFilter stoppingEvent, ILogger logger)
            : base(endpointInfo, settings, logger)
        {
            _stoppingEvent = stoppingEvent;
        }

        protected override EventTracePipeline CreatePipeline(Stream outputStream)
        {
            DiagnosticsClient client = new(EndpointInfo.Endpoint);
            return new EventTracePipeline(client, _settings,
                async (eventStream, token) =>
                {
                    _eventStreamAvailableCompletionSource?.TrySetResult(null);

                    await using EventMonitoringPassthroughStream eventMonitoringStream = new(
                        _stoppingEvent.ProviderName,
                        _stoppingEvent.EventName,
                        _stoppingEvent.PayloadFilter,
                        onEvent: (traceEvent) =>
                        {
                            Logger.StoppingTraceEventHit(traceEvent);
                            _stoppingEventHitSource.TrySetResult(null);
                        },
                        onPayloadFilterMismatch: Logger.StoppingTraceEventPayloadFilterMismatch,
                        eventStream,
                        outputStream,
                        DefaultBufferSize,
                        callOnEventOnlyOnce: true,
                        leaveDestinationStreamOpen: true /* We do not have ownership of the outputStream */);

                    await eventMonitoringStream.ProcessAsync(token);
                });
        }

        protected override async Task<Task> StartPipelineAsync(EventTracePipeline pipeline, CancellationToken token)
        {
            Task pipelineRunTask = pipeline.RunAsync(token);
            await _eventStreamAvailableCompletionSource.Task;

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
