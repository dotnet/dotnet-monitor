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
        private readonly StartupHookService _startupHookService;

        public ExceptionsService(
            IEndpointInfo endpointInfo,
            IOptions<ExceptionsOptions> options,
            IExceptionsStore store,
            StartupHookService startupHookService)
        {
            ArgumentNullException.ThrowIfNull(endpointInfo);
            ArgumentNullException.ThrowIfNull(store);

            _options = options ?? throw new ArgumentNullException(nameof(options));
            _startupHookService = startupHookService ?? throw new ArgumentNullException(nameof(startupHookService));

            _pipeline = new EventExceptionsPipeline(
                new DiagnosticsClient(endpointInfo.Endpoint),
                new EventExceptionsPipelineSettings(),
                store);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.Value.GetEnabled() || !await _startupHookService.CheckHasStartupHookAsync(stoppingToken))
            {
                return;
            }

            // Collect exceptions and place them into exceptions store
            await _pipeline.RunAsync(stoppingToken);
        }

        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();

            await _pipeline.DisposeAsync();
        }
    }
}
