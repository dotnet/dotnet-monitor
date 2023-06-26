// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi.Stacks;
using Microsoft.Diagnostics.NETCore.Client;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal enum StackFormat
    {
        Json,
        PlainText,
        Speedscope
    }

    internal static class StackUtilities
    {
        public static string GenerateStacksFilename(IEndpointInfo endpointInfo, bool plainText)
        {
            string extension = plainText ? "txt" : "json";
            return FormattableString.Invariant($"{Utilities.GetFileNameTimeStampUtcNow()}_{endpointInfo.ProcessId}.stacks.{extension}");
        }

        public static async Task CollectStacksAsync(TaskCompletionSource<object> startCompletionSource,
            IEndpointInfo endpointInfo,
            ProfilerChannel profilerChannel,
            StackFormat format,
            Stream outputStream, CancellationToken token)
        {
            var settings = new EventStacksPipelineSettings
            {
                Duration = Timeout.InfiniteTimeSpan
            };
            await using var eventTracePipeline = new EventStacksPipeline(new DiagnosticsClient(endpointInfo.Endpoint), settings);

            Task runPipelineTask = await eventTracePipeline.StartAsync(token);

            //CONSIDER Should we set this before or after the profiler message has been sent.
            startCompletionSource?.TrySetResult(null);

            await profilerChannel.SendMessage(
                endpointInfo,
                new SimpleProfilerMessage { MessageType = ProfilerMessageType.Callstack, Parameter = 0 },
                token);

            await runPipelineTask;
            Stacks.CallStackResult result = await eventTracePipeline.Result;

            StacksFormatter formatter = CreateFormatter(format, outputStream);

            await formatter.FormatStack(result, token);
        }

        private static StacksFormatter CreateFormatter(StackFormat format, Stream outputStream) =>
            format switch
            {
                StackFormat.Json => new JsonStacksFormatter(outputStream),
                StackFormat.Speedscope => new SpeedscopeStacksFormatter(outputStream),
                StackFormat.PlainText => new TextStacksFormatter(outputStream),
                _ => throw new InvalidOperationException(),
            };
    }
}
