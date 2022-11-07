// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Utils = Microsoft.Diagnostics.Monitoring.WebApi.Utilities;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class TraceOperation : AbstractTraceOperation
    {
        public TraceOperation(IEndpointInfo endpointInfo, EventTracePipelineSettings settings, ILogger logger)
            : base(endpointInfo, settings, logger) { }

        public override async Task ExecuteAsync(Stream outputStream, TaskCompletionSource<object> startCompletionSource, CancellationToken token)
        {
            _logger.StartCollectArtifact(Utils.ArtifactType_Trace);

            DiagnosticsClient client = new(_endpointInfo.Endpoint);

            await using EventTracePipeline pipeProcessor = new EventTracePipeline(client, _settings,
                async (eventStream, token) =>
                {
                    startCompletionSource.TrySetResult(null);
                    await eventStream.CopyToAsync(outputStream, DefaultBufferSize, token);
                });

            await pipeProcessor.RunAsync(token);
        }
    }
}
