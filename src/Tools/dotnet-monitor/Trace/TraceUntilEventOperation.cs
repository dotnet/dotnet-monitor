// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Utils = Microsoft.Diagnostics.Monitoring.WebApi.Utilities;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class TraceUntilEventOperation : AbstractTraceOperation
    {
        private readonly string _providerName;
        private readonly string _eventName;
        private readonly IDictionary<string, string> _payloadFilter;

        public TraceUntilEventOperation(IEndpointInfo endpointInfo, EventTracePipelineSettings settings, string providerName, string eventName, IDictionary<string, string> payloadFilter, ILogger logger)
            : base(endpointInfo, settings, logger)
        {
            _providerName = providerName;
            _eventName = eventName;
            _payloadFilter = payloadFilter;
        }

        public override async Task ExecuteAsync(Stream outputStream, TaskCompletionSource<object> startCompletionSource, CancellationToken token)
        {
            _logger.StartCollectArtifact(Utils.ArtifactType_Trace);

            DiagnosticsClient client = new(_endpointInfo.Endpoint);
            TaskCompletionSource<object> stoppingEventHitSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

            using IDisposable registration = token.Register(
                () => stoppingEventHitSource.TrySetCanceled(token));

            await using EventTracePipeline pipeProcessor = new(client, _settings,
                async (eventStream, token) =>
                {
                    startCompletionSource?.TrySetResult(null);

                    await using EventMonitoringPassthroughStream eventMonitoringStream = new(
                        _providerName,
                        _eventName,
                        _payloadFilter,
                        onEvent: (traceEvent) =>
                        {
                            _logger.StoppingTraceEventHit(traceEvent);
                            stoppingEventHitSource.TrySetResult(null);
                        },
                        onPayloadFilterMismatch: _logger.StoppingTraceEventPayloadFilterMismatch,
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
                await pipelineRunTask;
            }
        }
    }
}
