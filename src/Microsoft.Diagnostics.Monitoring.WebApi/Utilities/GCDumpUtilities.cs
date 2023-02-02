// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using FastSerialization;
using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.NETCore.Client;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal static class GCDumpUtilities
    {
        public static string GenerateGCDumpFileName(IEndpointInfo endpointInfo)
        {
            return FormattableString.Invariant($"{Utilities.GetFileNameTimeStampUtcNow()}_{endpointInfo.ProcessId}.gcdump");
        }

        public static async Task CaptureGCDumpAsync(IEndpointInfo endpointInfo, Stream targetStream, CancellationToken token)
        {
            await WriteToStream(await GetGCDumpAsync(endpointInfo, token), targetStream, token);
        }

        private static async Task<GCHeapDump> GetGCDumpAsync(IEndpointInfo endpointInfo, CancellationToken token)
        {
            var graph = new Graphs.MemoryGraph(50_000);

            EventGCPipelineSettings settings = new EventGCPipelineSettings
            {
                Duration = Timeout.InfiniteTimeSpan,
            };

            var client = new DiagnosticsClient(endpointInfo.Endpoint);

            await using var pipeline = new EventGCDumpPipeline(client, settings, graph);
            await pipeline.RunAsync(token);

            return new GCHeapDump(graph)
            {
                CreationTool = "dotnet-monitor"
            };
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
