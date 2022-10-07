// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Utils = Microsoft.Diagnostics.Monitoring.WebApi.Utilities;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class LogsOperation : IArtifactOperation
    {
        private readonly IEndpointInfo _endpointInfo;
        private readonly LogFormat _format;
        private readonly ILogger _logger;
        private readonly EventLogsPipelineSettings _settings;

        public LogsOperation(IEndpointInfo endpointInfo, EventLogsPipelineSettings settings, LogFormat format, ILogger logger)
        {
            _endpointInfo = endpointInfo;
            _format = format;
            _logger = logger;
            _settings = settings;
        }

        public async Task ExecuteAsync(
            Stream outputStream,
            TaskCompletionSource<object> startCompletionSource,
            CancellationToken token)
        {
            LoggerFactory loggerFactory = new();
            loggerFactory.AddProvider(new StreamingLoggerProvider(outputStream, _format, logLevel: null));

            var client = new DiagnosticsClient(_endpointInfo.Endpoint);

            await using EventLogsPipeline _pipeline = new(client, _settings, loggerFactory);

            Task runTask = await _pipeline.StartAsync(token);

            _logger.StartCollectArtifact(Utils.ArtifactType_Logs);

            // Signal that the logs operation has started
            startCompletionSource?.TrySetResult(null);

            await runTask;
        }

        public string GenerateFileName()
        {
            return FormattableString.Invariant($"{Utils.GetFileNameTimeStampUtcNow()}_{_endpointInfo.ProcessId}.txt");
        }

        public string ContentType => _format switch
        {
            LogFormat.PlainText => ContentTypes.TextPlain,
            LogFormat.NewlineDelimitedJson => ContentTypes.ApplicationNdJson,
            LogFormat.JsonSequence => ContentTypes.ApplicationJsonSequence,
            _ => ContentTypes.TextPlain
        };
    }
}
