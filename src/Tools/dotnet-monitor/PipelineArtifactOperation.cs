// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal abstract class PipelineArtifactOperation<T> :
        IArtifactOperation
        where T : Pipeline
    {
        private readonly string _artifactType;

        private Func<CancellationToken, Task> _stopFunc;

        protected PipelineArtifactOperation(ILogger logger, string artifactType, IEndpointInfo endpointInfo, bool isStoppable = true)
        {
            _artifactType = artifactType;

            Logger = logger;
            EndpointInfo = endpointInfo;
            IsStoppable = isStoppable;
        }

        public async Task ExecuteAsync(Stream outputStream, TaskCompletionSource<object> startCompletionSource, CancellationToken token)
        {
            await using T pipeline = CreatePipeline(outputStream);

            _stopFunc = pipeline.StopAsync;

            Task runTask = await StartPipelineAsync(pipeline, token);

            Logger.StartCollectArtifact(_artifactType);

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

        protected abstract T CreatePipeline(Stream outputStream);

        /// <summary>
        /// Starts the pipeline and returns a <see cref="Task{Task}"/> that completes when the pipeline
        /// has started; the inner <see cref="Task"/> shall complete when the pipeline runs to completion.
        /// </summary>
        /// <param name="pipeline">The pipeline that shall be started.</param>
        /// <param name="token">The token to monitor for cancellation requests.</param>
        protected abstract Task<Task> StartPipelineAsync(T pipeline, CancellationToken token);

        protected IEndpointInfo EndpointInfo { get; }

        protected ILogger Logger { get; }
    }
}
