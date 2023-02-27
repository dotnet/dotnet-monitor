// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Exceptions;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Extensions.Hosting;
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
        private readonly IExceptionsStore _exceptionsStore;
        private readonly IDiagnosticServices _diagnosticServices;
        private readonly IInProcessFeatures _inProcessFeatures;

        private EventExceptionsPipeline _pipeline;

        public ExceptionsService(
            IDiagnosticServices diagnosticServices,
            IInProcessFeatures inProcessFeatures,
            IExceptionsStore exceptionsStore)
        {
            _diagnosticServices = diagnosticServices;
            _exceptionsStore = exceptionsStore;
            _inProcessFeatures = inProcessFeatures;
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
                    await Task.Delay(5000, stoppingToken);
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
    }
}
