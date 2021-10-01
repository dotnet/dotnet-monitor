// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using FastSerialization;
using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.WebApi.Validation;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public static TimeSpan ConvertSecondsToTimeSpan(int durationSeconds)
        {
            return durationSeconds < 0 ?
                Timeout.InfiniteTimeSpan :
                TimeSpan.FromSeconds(durationSeconds);
        }

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

        public static string GenerateTraceFileName(IEndpointInfo endpointInfo)
        {
            return FormattableString.Invariant($"{GetFileNameTimeStampUtcNow()}_{endpointInfo.ProcessId}.nettrace");
        }

        public static KeyValueLogScope CreateArtifactScope(string artifactType, IEndpointInfo endpointInfo)
        {
            KeyValueLogScope scope = new KeyValueLogScope();
            scope.AddArtifactType(artifactType);
            scope.AddArtifactEndpointInfo(endpointInfo);
            return scope;
        }

        public static MonitoringSourceConfiguration GetTraceConfiguration(Models.TraceProfile profile, int metricsIntervalSeconds)
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