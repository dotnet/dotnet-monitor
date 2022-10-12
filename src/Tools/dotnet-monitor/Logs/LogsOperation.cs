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
using Utils = Microsoft.Diagnostics.Monitoring.WebApi.Utilities;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class LogsOperation : EventSourceArtifactOperation<EventLogsPipelineSettings>
    {
        private readonly LogFormat _format;

        public LogsOperation(IEndpointInfo endpointInfo, EventLogsPipelineSettings settings, LogFormat format, ILogger logger)
            : base(logger, Utils.ArtifactType_Logs, endpointInfo, settings)
        {
            _format = format;
        }

        protected override EventSourcePipeline<EventLogsPipelineSettings> CreatePipeline(Stream outputStream)
        {
            LoggerFactory loggerFactory = new();
            loggerFactory.AddProvider(new StreamingLoggerProvider(outputStream, _format, logLevel: null));

            var client = new DiagnosticsClient(EndpointInfo.Endpoint);

            return new EventLogsPipeline(client, Settings, loggerFactory);
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
