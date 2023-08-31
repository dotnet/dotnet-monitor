// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using FastSerialization;
using Graphs;
using Microsoft.Diagnostics.Monitoring;
using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Utils = Microsoft.Diagnostics.Monitoring.WebApi.Utilities;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class GCDumpOperation : PipelineArtifactOperation<GCDumpOperation.GCDumpOperationPipeline>
    {
        public GCDumpOperation(IEndpointInfo endpointInfo, OperationTrackerService trackerService, ILogger logger)
            : base(trackerService, logger, Utils.ArtifactType_GCDump, endpointInfo, isStoppable: false, register: true)
        {
        }

        public override string GenerateFileName()
        {
            return FormattableString.Invariant($"{Utils.GetFileNameTimeStampUtcNow()}_{EndpointInfo.ProcessId}.gcdump");
        }

        protected override GCDumpOperationPipeline CreatePipeline(Stream outputStream)
        {
            return new GCDumpOperationPipeline(EndpointInfo, outputStream);
        }

        protected override Task<Task> StartPipelineAsync(GCDumpOperationPipeline pipeline, CancellationToken token)
        {
            return pipeline.StartAsync(token);
        }

        public override string ContentType => ContentTypes.ApplicationOctetStream;

        internal sealed class GCDumpOperationPipeline : Pipeline
        {
            private readonly MemoryGraph _graph = new(50_000);
            private readonly Stream _outputStream;
            private readonly EventGCDumpPipeline _pipeline;

            public GCDumpOperationPipeline(IEndpointInfo endpointInfo, Stream outputStream)
            {
                _outputStream = outputStream;

                EventGCPipelineSettings settings = new()
                {
                    Duration = Timeout.InfiniteTimeSpan
                };

                _pipeline = new EventGCDumpPipeline(
                    new NETCore.Client.DiagnosticsClient(endpointInfo.Endpoint),
                    settings,
                    _graph);
            }

            public async Task<Task> StartAsync(CancellationToken token)
            {
                // Make sure this pipeline is running so its OnRun can process data
                // after the run of the inner pipeline completes. Then await the
                // start of the inner pipeline before returning the run Task.
                Task runTask = RunAsync(token);

                _ = await _pipeline.StartAsync(token);

                return runTask;
            }

            protected override async Task OnRun(CancellationToken token)
            {
                await _pipeline.RunAsync(token);

                GCHeapDump dump = new(_graph)
                {
                    CreationTool = "dotnet-monitor"
                };

                await WriteToStream(dump, _outputStream, token);
            }

            protected override async Task OnStop(CancellationToken token)
            {
                await _pipeline.StopAsync(token);
            }

            protected override async Task OnCleanup()
            {
                await _pipeline.DisposeAsync();
            }

            private static async Task WriteToStream(IFastSerializable serializable, Stream targetStream, CancellationToken token)
            {
                // FastSerialization requests the length of the stream before serializing to the stream.
                // If the stream is a response stream, requesting the length or setting the position is
                // not supported. Create an intermediate buffer if testing the stream fails.
                // This can use a huge amount of memory if the IFastSerializable is very large.
                // CONSIDER: Update FastSerialization to not get the length or attempt to reset the position.
                bool useIntermediateStream = false;
                try
                {
                    _ = targetStream.Length;
                }
                catch (NotSupportedException)
                {
                    useIntermediateStream = true;
                }

                if (useIntermediateStream)
                {
                    using var intermediateStream = new MemoryStream();

                    var serializer = new Serializer(intermediateStream, serializable, leaveOpen: true);
                    serializer.Close();

                    intermediateStream.Position = 0;

                    await intermediateStream.CopyToAsync(targetStream, 0x10000, token);
                }
                else
                {
                    var serializer = new Serializer(targetStream, serializable, leaveOpen: true);
                    serializer.Close();
                }
            }
        }
    }
}
