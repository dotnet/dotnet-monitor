// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal static class LogsUtilities
    {
        public static string GetLogsContentType(LogFormat format)
        {
            if (format == LogFormat.PlainText)
            {
                return ContentTypes.TextPlain;
            }
            else if (format == LogFormat.NewlineDelimitedJson)
            {
                return ContentTypes.ApplicationNdJson;
            }
            else if (format == LogFormat.JsonSequence)
            {
                return ContentTypes.ApplicationJsonSequence;
            }
            else
            {
                return ContentTypes.TextPlain;
            }
        }

        public static async Task CaptureLogsAsync(TaskCompletionSource<object> startCompletionSource, LogFormat format, IEndpointInfo endpointInfo, EventLogsPipelineSettings settings, Stream outputStream, CancellationToken token)
        {
            using var loggerFactory = new LoggerFactory();

            await using FileBufferingWriteStream bufferingStream = new();

            loggerFactory.AddProvider(new StreamingLoggerProvider(bufferingStream, format, logLevel: null));

            var client = new DiagnosticsClient(endpointInfo.Endpoint);

            await using EventLogsPipeline pipeline = new EventLogsPipeline(client, settings, loggerFactory);

            Task runTask = await pipeline.StartAsync(token);

            if (null != startCompletionSource)
            {
                startCompletionSource.TrySetResult(null);
            }

            await runTask;
            await bufferingStream.DrainBufferAsync(outputStream, token);
        }

        public static string GenerateLogsFileName(IEndpointInfo endpointInfo)
        {
            return FormattableString.Invariant($"{Utilities.GetFileNameTimeStampUtcNow()}_{endpointInfo.ProcessId}.txt");
        }
    }
}
