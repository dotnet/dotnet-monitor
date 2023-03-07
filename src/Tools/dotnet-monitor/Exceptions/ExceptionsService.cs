// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Exceptions;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tools.Monitor.StartupHook;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
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

        private readonly List<UniqueProcessKey> _unconfiguredProcesses = new();
        private readonly IExceptionsStore _exceptionsStore;
        private readonly IDiagnosticServices _diagnosticServices;
        private readonly IInProcessFeatures _inProcessFeatures;
        private readonly StartupHookValidator _startupHookValidator;

        private EventExceptionsPipeline _pipeline;

        public ExceptionsService(
            StartupHookValidator startupHookValidator,
            IDiagnosticServices diagnosticServices,
            IInProcessFeatures inProcessFeatures,
            IExceptionsStore exceptionsStore)
        {
            _diagnosticServices = diagnosticServices;
            _exceptionsStore = exceptionsStore;
            _inProcessFeatures = inProcessFeatures;
            _startupHookValidator = startupHookValidator;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_inProcessFeatures.IsExceptionsEnabled)
            {
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Get default process
                    IProcessInfo pi = await _diagnosticServices.GetProcessAsync(processKey: null, stoppingToken);

                    // If previous checked configuration and it did not pass, do not check again or attempt
                    // to start the event pipe session.
                    UniqueProcessKey key = new(pi.EndpointInfo.ProcessId, pi.EndpointInfo.RuntimeInstanceCookie);
                    if (_unconfiguredProcesses.Contains(key))
                    {
                        // This exception is not user visible.
                        throw new NotSupportedException();
                    }

                    // Validate that the process is configured correctly for collecting exceptions.
                    if (!await _startupHookValidator.CheckAsync(pi.EndpointInfo, stoppingToken))
                    {
                        _unconfiguredProcesses.Add(key);

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
