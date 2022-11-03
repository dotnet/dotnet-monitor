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

        private EventSourcePipeline<T> _pipeline;
        private Task _runTask;

        protected EventSourceArtifactOperation(ILogger logger, string artifactType, IEndpointInfo endpointInfo, T settings, bool isStoppable = true)
        {
            _artifactType = artifactType;
            _logger = logger;

            EndpointInfo = endpointInfo;
            IsStoppable = isStoppable;
            Settings = settings;
        }

        public ValueTask DisposeAsync()
        {
            if (null != _pipeline)
            {
                return _pipeline.DisposeAsync();
            }
            return ValueTask.CompletedTask;
        }

        public async Task StartAsync(Stream outputStream, CancellationToken token)
        {
            if (null != _pipeline || null != _runTask)
            {
                throw new InvalidOperationException();
            }

            _pipeline = CreatePipeline(outputStream);

            _runTask = await _pipeline.StartAsync(token);

            _logger.StartCollectArtifact(_artifactType);
        }

        public Task StopAsync(CancellationToken token)
        {
            if (null == _pipeline)
            {
                throw new InvalidOperationException();
            }

            if (!IsStoppable)
            {
                throw new MonitoringException(Strings.ErrorMessage_OperationIsNotStoppable);
            }

            return _pipeline.StopAsync(token);
        }

        public Task WaitForCompletionAsync(CancellationToken token)
        {
            if (null == _runTask)
            {
                throw new InvalidOperationException();
            }

            return _runTask.WaitAsync(token);
        }

        public abstract string GenerateFileName();

        public abstract string ContentType { get; }

        public bool IsStoppable { get; }

        protected abstract EventSourcePipeline<T> CreatePipeline(Stream outputStream);

        protected IEndpointInfo EndpointInfo { get; }

        protected T Settings { get; }
    }
}
