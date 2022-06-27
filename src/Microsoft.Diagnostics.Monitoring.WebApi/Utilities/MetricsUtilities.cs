// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Diagnostics.NETCore.Client;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal static class MetricsUtilities
    {
        public static async Task CaptureLiveCustomMetricsAsync(TaskCompletionSource<object> startCompletionSource, IEndpointInfo endpointInfo, GlobalCounterOptions counterOptions, int durationSeconds, EventMetricsConfiguration configuration, Stream outputStream, CancellationToken token)
        {
            var client = new DiagnosticsClient(endpointInfo.Endpoint);

            EventPipeCounterPipelineSettings settings = EventCounterSettingsFactory.CreateSettings(
                counterOptions,
                durationSeconds,
                configuration);

            await using EventCounterPipeline eventCounterPipeline = new EventCounterPipeline(client,
                settings,
                loggers:
                new[] { new JsonCounterLogger(outputStream) });

            await eventCounterPipeline.RunAsync(token);
        }

        public static async Task CaptureLiveMetricsAsync(TaskCompletionSource<object> startCompletionSource, IEndpointInfo endpointInfo, GlobalCounterOptions counterOptions, int durationSeconds, Stream outputStream, CancellationToken token)
        {
            var client = new DiagnosticsClient(endpointInfo.Endpoint);
            EventPipeCounterPipelineSettings settings = EventCounterSettingsFactory.CreateSettings(
                counterOptions,
                includeDefaults: true,
                durationSeconds: durationSeconds);

            await using EventCounterPipeline eventCounterPipeline = new EventCounterPipeline(client,
                settings,
                loggers:
                new[] { new JsonCounterLogger(outputStream) });

            await eventCounterPipeline.RunAsync(token);
        }

        public static string GetMetricFilename(IEndpointInfo endpointInfo) =>
            FormattableString.Invariant($"{Utilities.GetFileNameTimeStampUtcNow()}_{endpointInfo.ProcessId}.metrics.json");
    }
}
