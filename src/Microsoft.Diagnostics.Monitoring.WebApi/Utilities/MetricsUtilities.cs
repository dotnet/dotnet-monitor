// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal static class MetricsUtilities
    {
        public static async Task CaptureLiveMetricsAsync(TaskCompletionSource<object> startCompletionSource, IEndpointInfo endpointInfo, Stream outputStream, CancellationToken token)
        {
            /*
            using var loggerFactory = new LoggerFactory();

            loggerFactory.AddProvider(new StreamingLoggerProvider(outputStream, format, logLevel: null));

            var client = new DiagnosticsClient(endpointInfo.Endpoint);

            await using EventLogsPipeline pipeline = new EventLogsPipeline(client, settings, loggerFactory);

            Task runTask = await pipeline.StartAsync(token);

            if (null != startCompletionSource)
            {
                startCompletionSource.TrySetResult(null);
            }

            await runTask;*/
        }

        public static string GetMetricFilename(IEndpointInfo endpointInfo) =>
            FormattableString.Invariant($"{Utilities.GetFileNameTimeStampUtcNow()}_{endpointInfo.ProcessId}.metrics.json");
    }
}
