// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring;
using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal abstract class EventSourceArtifactOperation<T> :
        IArtifactOperation
        where T : EventSourcePipelineSettings
    {
        private readonly string _artifactType;
        private readonly ILogger _logger;

        private Func<CancellationToken, Task> _stopFunc;

        protected EventSourceArtifactOperation(ILogger logger, string artifactType, IEndpointInfo endpointInfo, T settings, bool isStoppable = true)
        {
            _artifactType = artifactType;
            _logger = logger;

            EndpointInfo = endpointInfo;
            IsStoppable = isStoppable;
            Settings = settings;
        }

        public async Task ExecuteAsync(Stream outputStream, TaskCompletionSource<object> startCompletionSource, CancellationToken token)
        {
            await using EventSourcePipeline<T> pipeline = CreatePipeline(outputStream);

            _stopFunc = pipeline.StopAsync;

            Task runTask = await pipeline.StartAsync(token);

            _logger.StartCollectArtifact(_artifactType);

            // Signal that the logs operation has started
            startCompletionSource?.TrySetResult(null);

            await runTask;
        }

        public async Task StopAsync(CancellationToken token)
        {
            if (null == _stopFunc)
            {
                throw new InvalidOperationException();
            }

            if (!IsStoppable)
            {
                throw new MonitoringException(Strings.ErrorMessage_OperationIsNotStoppable);
            }

            await _stopFunc(token);
        }

        public abstract string GenerateFileName();

        public abstract string ContentType { get; }

        public bool IsStoppable { get; }

        protected abstract EventSourcePipeline<T> CreatePipeline(Stream outputStream);

        protected IEndpointInfo EndpointInfo { get; }

        protected T Settings { get; }
    }
}
