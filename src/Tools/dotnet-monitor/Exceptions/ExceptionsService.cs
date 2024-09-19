// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Exceptions;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tools.Monitor.StartupHook;
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
        DiagnosticLifetimeBackgroundService
    {
        private readonly EventExceptionsPipeline _pipeline;
        private readonly IOptions<ExceptionsOptions> _options;

        public ExceptionsService(
            IEndpointInfo endpointInfo,
            IOptions<ExceptionsOptions> options,
            IExceptionsStore store,
            StartupHookService startupHookService)
        {
            ArgumentNullException.ThrowIfNull(endpointInfo);
            ArgumentNullException.ThrowIfNull(store);

            _options = options ?? throw new ArgumentNullException(nameof(options));

            _pipeline = new EventExceptionsPipeline(
                new DiagnosticsClient(endpointInfo.Endpoint),
                new EventExceptionsPipelineSettings(),
                store);
        }


        protected override async Task<Task> ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.Value.GetEnabled())
            {
                return Task.CompletedTask;
            }

            // Collect exceptions and place them into exceptions store
            return await _pipeline.StartAsync(stoppingToken);
        }

        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();

            await _pipeline.DisposeAsync();
        }
    }
}
