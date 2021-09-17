// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using FastSerialization;
using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.NETCore.Client;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal static class Utilities
    {
        public const string ArtifactType_Dump = "dump";
        public const string ArtifactType_GCDump = "gcdump";
        public const string ArtifactType_Logs = "logs";
        public const string ArtifactType_Trace = "trace";
        public const string ArtifactType_Metrics = "livemetrics";

        public static string GenerateDumpFileName()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                FormattableString.Invariant($"dump_{GetFileNameTimeStampUtcNow()}.dmp") :
                FormattableString.Invariant($"core_{GetFileNameTimeStampUtcNow()}");
        }

        public static string GenerateGCDumpFileName(IEndpointInfo endpointInfo)
        {
            return FormattableString.Invariant($"{GetFileNameTimeStampUtcNow()}_{endpointInfo.ProcessId}.gcdump");
        }

        public static string GetFileNameTimeStampUtcNow()
        {
            return DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        }

        public static KeyValueLogScope CreateArtifactScope(string artifactType, IEndpointInfo endpointInfo)
        {
            KeyValueLogScope scope = new KeyValueLogScope();
            scope.AddArtifactType(artifactType);
            scope.AddArtifactEndpointInfo(endpointInfo);
            return scope;
        }

        public static Func<Stream, CancellationToken, Task> ConvertFastSerializeAction(Func<CancellationToken, Task<IFastSerializable>> action)
        {
            return async (stream, token) =>
            {
                IFastSerializable fastSerializable = await action(token);

                // FastSerialization requests the length of the stream before serializing to the stream.
                // If the stream is a response stream, requesting the length or setting the position is
                // not supported. Create an intermediate buffer if testing the stream fails.
                // This can use a huge amount of memory if the IFastSerializable is very large.
                // CONSIDER: Update FastSerialization to not get the length or attempt to reset the position.
                bool useIntermediateStream = false;
                try
                {
                    _ = stream.Length;
                }
                catch (NotSupportedException)
                {
                    useIntermediateStream = true;
                }

                if (useIntermediateStream)
                {
                    using var intermediateStream = new MemoryStream();

                    var serializer = new Serializer(intermediateStream, fastSerializable, leaveOpen: true);
                    serializer.Close();

                    intermediateStream.Position = 0;

                    await intermediateStream.CopyToAsync(stream, 0x10000, token);
                }
                else
                {
                    var serializer = new Serializer(stream, fastSerializable, leaveOpen: true);
                    serializer.Close();
                }
            };
        }

        internal static Func<CancellationToken, Task<IFastSerializable>> GetGCHeadDump(IEndpointInfo endpointInfo)
        {
            return async (token) =>
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
            };
        }
    }
}
