// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Exceptions;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.Exceptions
{
    /// <summary>
    /// Get exception information from target process and store it.
    /// </summary>
    internal sealed class ExceptionsService :
        IDiagnosticLifetimeService, IAsyncDisposable
    {
        private readonly EventExceptionsPipeline _pipeline;
        private readonly IOptions<ExceptionsOptions> _options;
        // We don't need to guard against concurrent StartAsync and StopAsync calls
        private bool _isStarted;

        public ExceptionsService(
            IEndpointInfo endpointInfo,
            IOptions<ExceptionsOptions> options,
            IExceptionsStore store)
        {
            ArgumentNullException.ThrowIfNull(endpointInfo);
            ArgumentNullException.ThrowIfNull(store);

            _options = options ?? throw new ArgumentNullException(nameof(options));

            _pipeline = new EventExceptionsPipeline(
                new DiagnosticsClient(endpointInfo.Endpoint),
                new EventExceptionsPipelineSettings(),
                store);
        }

        public async ValueTask StartAsync(CancellationToken cancellationToken)
        {
            if (!_options.Value.GetEnabled())
            {
                return;
            }

            // Wrap the passed CancellationToken into a linked CancellationTokenSource so that the
            // RunAsync method is only cancellable for the execution of the StartAsync method.
            // We don't want the caller to be able to cancel the run of the pipeline after having finished
            // executing the StartAsync method.
            using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // Collect exceptions and place them into exceptions store
            await _pipeline.StartAsync(cts.Token);
            _isStarted = true;
        }

        public async ValueTask StopAsync(CancellationToken cancellationToken)
        {
            if (!_isStarted)
            {
                return;
            }

            await _pipeline.StopAsync(cancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            await _pipeline.DisposeAsync();
        }
    }
}
