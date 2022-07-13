// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal static class MetricsUtilities
    {
        public static async Task CaptureLiveMetricsAsync(TaskCompletionSource<object> startCompletionSource, IEndpointInfo endpointInfo, EventPipeCounterPipelineSettings settings, Stream outputStream, CancellationToken token)
        {
            var client = new DiagnosticsClient(endpointInfo.Endpoint);
         
            await using EventCounterPipeline eventCounterPipeline = new EventCounterPipeline(client,
                settings,
                loggers:
                new[] { new JsonCounterLogger(outputStream) });

            Task runTask = await eventCounterPipeline.StartAsync(token);

            if (null != startCompletionSource)
            {
                startCompletionSource.TrySetResult(null);
            }

            await runTask;
        }

        public static string GetMetricFilename(IEndpointInfo endpointInfo) =>
            FormattableString.Invariant($"{Utilities.GetFileNameTimeStampUtcNow()}_{endpointInfo.ProcessId}.metrics.json");
    }
}
