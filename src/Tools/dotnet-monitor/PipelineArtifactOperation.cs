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
        private readonly TaskCompletionSource _startCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        private Func<CancellationToken, Task>? _stopFunc;

        protected OperationTrackerService OperationTrackerService { get; }

        protected PipelineArtifactOperation(OperationTrackerService trackerService, ILogger logger, string artifactType, IEndpointInfo endpointInfo, bool isStoppable = true, bool register = false)
        {
            _artifactType = artifactType;
            OperationTrackerService = trackerService;

            Logger = logger;
            EndpointInfo = endpointInfo;
            IsStoppable = isStoppable;
            Register = register;
        }

        public async Task ExecuteAsync(Stream outputStream, CancellationToken token)
        {
            try
            {
                using IDisposable _ = token.Register(() => _startCompletionSource.TrySetCanceled(token));

                await using T pipeline = CreatePipeline(outputStream);

                _stopFunc = pipeline.StopAsync;

                using IDisposable? trackerRegistration = Register ? OperationTrackerService.Register(EndpointInfo) : null;

                Task runTask = await StartPipelineAsync(pipeline, token);

                Logger.StartCollectArtifact(_artifactType);

                // Signal that the artifact operation has started
                _startCompletionSource.TrySetResult();

                await runTask;
            }
            catch (Exception ex)
            {
                _ = _startCompletionSource.TrySetException(ex);
                throw;
            }
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

        public bool Register { get; }

        public Task Started => _startCompletionSource.Task;

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
