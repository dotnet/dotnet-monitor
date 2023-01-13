// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.EventPipe;
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
    internal sealed class MetricsOperation : PipelineArtifactOperation<CounterPipeline>
    {
        private readonly CounterPipelineSettings _settings;

        public MetricsOperation(IEndpointInfo endpointInfo, CounterPipelineSettings settings, ILogger logger)
            : base(logger, Utils.ArtifactType_Metrics, endpointInfo)
        {
            _settings = settings;
        }

        protected override CounterPipeline CreatePipeline(Stream outputStream)
        {
            var client = new DiagnosticsClient(EndpointInfo.Endpoint);

            return new CounterPipeline(
                client,
                _settings,
                loggers: new[] { new JsonCounterLogger(outputStream, Logger) });
        }

        protected override Task<Task> StartPipelineAsync(CounterPipeline pipeline, CancellationToken token)
        {
            return pipeline.StartAsync(token);
        }

        public override string GenerateFileName()
        {
            return FormattableString.Invariant($"{Utils.GetFileNameTimeStampUtcNow()}_{EndpointInfo.ProcessId}.metrics.json");
        }

        public override string ContentType => ContentTypes.ApplicationJsonSequence;
    }
}
