// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.NETCore.Client;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal static class MetricsUtilities
    {
        public static async Task CaptureLiveMetricsAsync(TaskCompletionSource<object> startCompletionSource, IEndpointInfo endpointInfo, EventPipeCounterPipelineSettings settings, Stream outputStream, CancellationToken token)
        {
            var client = new DiagnosticsClient(endpointInfo.Endpoint);

            await using FileBufferingWriteStream bufferingStream = new();

            await using EventCounterPipeline eventCounterPipeline = new EventCounterPipeline(client,
                settings,
                loggers:
                new[] { new JsonCounterLogger(bufferingStream) });

            Task runTask = await eventCounterPipeline.StartAsync(token);

            startCompletionSource?.TrySetResult(null);

            await runTask;
            await bufferingStream.DrainBufferAsync(outputStream, token);
        }

        public static string GetMetricFilename(IEndpointInfo endpointInfo) =>
            FormattableString.Invariant($"{Utilities.GetFileNameTimeStampUtcNow()}_{endpointInfo.ProcessId}.metrics.json");
    }
}
