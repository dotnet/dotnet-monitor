// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    internal sealed class LogsOperation : PipelineArtifactOperation<EventLogsPipeline>
    {
        private readonly LogFormat _format;
        private readonly EventLogsPipelineSettings _settings;

        public LogsOperation(IEndpointInfo endpointInfo, EventLogsPipelineSettings settings, LogFormat format, OperationTrackerService trackerService, ILogger logger)
            : base(trackerService, logger, Utils.ArtifactType_Logs, endpointInfo)
        {
            _format = format;
            _settings = settings;
        }

        protected override EventLogsPipeline CreatePipeline(Stream outputStream)
        {
            LoggerFactory loggerFactory = new();
            loggerFactory.AddProvider(new StreamingLoggerProvider(outputStream, _format, logLevel: null));

            var client = new DiagnosticsClient(EndpointInfo.Endpoint);

            return new EventLogsPipeline(client, _settings, loggerFactory);
        }

        protected override Task<Task> StartPipelineAsync(EventLogsPipeline pipeline, CancellationToken token)
        {
            return pipeline.StartAsync(token);
        }

        public override string GenerateFileName()
        {
            return FormattableString.Invariant($"{Utils.GetFileNameTimeStampUtcNow()}_{EndpointInfo.ProcessId}.txt");
        }

        public override string ContentType => _format switch
        {
            LogFormat.PlainText => ContentTypes.TextPlain,
            LogFormat.NewlineDelimitedJson => ContentTypes.ApplicationNdJson,
            LogFormat.JsonSequence => ContentTypes.ApplicationJsonSequence,
            _ => ContentTypes.TextPlain
        };
    }
}
