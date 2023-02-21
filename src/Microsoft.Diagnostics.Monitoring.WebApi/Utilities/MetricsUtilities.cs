// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        public static async Task CaptureLiveMetricsAsync(TaskCompletionSource<object> startCompletionSource, IEndpointInfo endpointInfo, MetricsPipelineSettings settings, Stream outputStream, CancellationToken token)
        {
            var client = new DiagnosticsClient(endpointInfo.Endpoint);

            await using MetricsPipeline eventCounterPipeline = new MetricsPipeline(client,
                settings,
                loggers:
                new[] { new JsonCounterLogger(outputStream) });

            Task runTask = await eventCounterPipeline.StartAsync(token);

            startCompletionSource?.TrySetResult(null);

            await runTask;
        }

        public static string GetMetricFilename(IEndpointInfo endpointInfo) =>
            FormattableString.Invariant($"{Utilities.GetFileNameTimeStampUtcNow()}_{endpointInfo.ProcessId}.metrics.json");
    }
}
