// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Stacks;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Utils = Microsoft.Diagnostics.Monitoring.WebApi.Utilities;

namespace Microsoft.Diagnostics.Tools.Monitor.Stacks
{
    internal sealed class StacksOperation : PipelineArtifactOperation<StacksOperation.StacksOperationPipeline>
    {
        private readonly ProfilerChannel _channel;
        private readonly StackFormat _format;

        public StacksOperation(IEndpointInfo endpointInfo, StackFormat format, ProfilerChannel channel, OperationTrackerService trackerService, ILogger logger)
            : base(trackerService, logger, Utils.ArtifactType_Stacks, endpointInfo, isStoppable: false)
        {
            _channel = channel;
            _format = format;
        }

        public override string GenerateFileName()
        {
            string extension = _format == StackFormat.PlainText ? "txt" : "json";
            return FormattableString.Invariant($"{Utils.GetFileNameTimeStampUtcNow()}_{EndpointInfo.ProcessId}.stacks.{extension}");
        }

        protected override StacksOperationPipeline CreatePipeline(Stream outputStream)
        {
            return new StacksOperationPipeline(EndpointInfo, _channel, _format, outputStream);
        }

        protected override Task<Task> StartPipelineAsync(StacksOperationPipeline pipeline, CancellationToken token)
        {
            return pipeline.StartAsync(token);
        }

        public override string ContentType => _format switch
        {
            StackFormat.PlainText => ContentTypes.TextPlain,
            StackFormat.Json => ContentTypes.ApplicationJson,
            StackFormat.Speedscope => ContentTypes.ApplicationSpeedscopeJson,
            _ => throw new InvalidOperationException()
        };

        internal sealed class StacksOperationPipeline : Pipeline
        {
            private readonly ProfilerChannel _channel;
            private readonly IEndpointInfo _endpointInfo;
            private readonly StackFormat _format;
            private readonly Stream _outputStream;
            private readonly EventStacksPipeline _pipeline;

            public StacksOperationPipeline(IEndpointInfo endpointInfo, ProfilerChannel channel, StackFormat format, Stream outputStream)
            {
                _channel = channel;
                _endpointInfo = endpointInfo;
                _format = format;
                _outputStream = outputStream;

                EventStacksPipelineSettings settings = new()
                {
                    Duration = Timeout.InfiniteTimeSpan
                };

                _pipeline = new EventStacksPipeline(new DiagnosticsClient(endpointInfo.Endpoint), settings);
            }

            public async Task<Task> StartAsync(CancellationToken token)
            {
                // Make sure this pipeline is running so its OnRun can process data
                // after the run of the inner pipeline completes. Then await the
                // start of the inner pipeline before sending the profiler message.
                Task runTask = RunAsync(token);

                _ = await _pipeline.StartAsync(token);

                await _channel.SendMessage(
                    _endpointInfo,
                    new CommandOnlyProfilerMessage(ProfilerCommand.Callstack),
                    token);

                return runTask;
            }

            protected override async Task OnRun(CancellationToken token)
            {
                await _pipeline.RunAsync(token);

                CallStackResult result = await _pipeline.Result;

                StacksFormatter formatter = _format switch
                {
                    StackFormat.Json => new JsonStacksFormatter(_outputStream),
                    StackFormat.Speedscope => new SpeedscopeStacksFormatter(_outputStream),
                    StackFormat.PlainText => new TextStacksFormatter(_outputStream),
                    _ => throw new InvalidOperationException(),
                };

                await formatter.FormatStack(result, token);
            }

            protected override async Task OnStop(CancellationToken token)
            {
                await _pipeline.StopAsync(token);
            }

            protected override async Task OnCleanup()
            {
                await _pipeline.DisposeAsync();
            }
        }
    }
}
