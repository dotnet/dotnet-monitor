// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Exceptions;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tools.Monitor.StartupHook;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.Exceptions
{
    /// <summary>
    /// Get exception information from default process and store it.
    /// </summary>
    internal sealed class ExceptionsService :
        BackgroundService
    {
        private readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

        private readonly IExceptionsStore _exceptionsStore;
        private readonly IDiagnosticServices _diagnosticServices;
        private readonly IOptions<ExceptionsOptions> _exceptionsOptions;
        private readonly StartupHookEndpointInfoSourceCallbacks _startupHookEndpointInfoSourceCallbacks;

        private EventExceptionsPipeline _pipeline;

        public ExceptionsService(
            IDiagnosticServices diagnosticServices,
            IOptions<ExceptionsOptions> exceptionsOptions,
            IExceptionsStore exceptionsStore,
            StartupHookEndpointInfoSourceCallbacks startupHookEndpointInfoSourceCallbacks)
        {
            _diagnosticServices = diagnosticServices;
            _exceptionsStore = exceptionsStore;
            _exceptionsOptions = exceptionsOptions;
            _startupHookEndpointInfoSourceCallbacks = startupHookEndpointInfoSourceCallbacks;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_exceptionsOptions.Value.GetEnabled())
            {
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Get default process
                    IProcessInfo pi = await _diagnosticServices.GetProcessAsync(processKey: null, stoppingToken);

                    bool isStartupHookApplied = false;
                    _ = _startupHookEndpointInfoSourceCallbacks.ApplyStartupState.TryGetValue(pi.EndpointInfo.RuntimeInstanceCookie, out isStartupHookApplied);

                    // Validate that the process is configured correctly for collecting exceptions.
                    if (!isStartupHookApplied)
                    {
                        // This exception is not user visible.
                        throw new NotSupportedException();
                    }

                    DiagnosticsClient client = new(pi.EndpointInfo.Endpoint);

                    EventExceptionsPipelineSettings settings = new();
                    _pipeline = new EventExceptionsPipeline(client, settings, _exceptionsStore);

                    // Monitor for exceptions
                    await _pipeline.RunAsync(stoppingToken);
                }
                catch (Exception e) when (e is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
                {
                    if (null != _pipeline)
                    {
                        await _pipeline.DisposeAsync();
                    }
                    await Task.Delay(RetryDelay, stoppingToken);
                }
            }
        }

        public override async void Dispose()
        {
            base.Dispose();
            if (null != _pipeline)
            {
                await _pipeline.DisposeAsync();
            }
        }

        private record class UniqueProcessKey(int ProcessId, Guid RuntimeInstanceId);
    }
}
