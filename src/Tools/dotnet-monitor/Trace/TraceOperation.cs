// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class TraceOperation : AbstractTraceOperation
    {
        private readonly TaskCompletionSource<object?> _eventStreamAvailableCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TraceOperation(IEndpointInfo endpointInfo, EventTracePipelineSettings settings, OperationTrackerService trackerService, ILogger logger)
            : base(endpointInfo, settings, trackerService, logger) { }

        protected override EventTracePipeline CreatePipeline(Stream outputStream)
        {
            DiagnosticsClient client = new(EndpointInfo.Endpoint);
            return new EventTracePipeline(client, _settings,
                async (eventStream, token) =>
                {
                    _eventStreamAvailableCompletionSource.TrySetResult(null);
                    await eventStream.CopyToAsync(outputStream, DefaultBufferSize, token);
                });
        }

        protected override async Task<Task> StartPipelineAsync(EventTracePipeline pipeline, CancellationToken token)
        {
            Task pipelineRunTask = pipeline.RunAsync(token);
            await _eventStreamAvailableCompletionSource.Task.WaitAsync(token);
            return pipelineRunTask;
        }
    }
}
