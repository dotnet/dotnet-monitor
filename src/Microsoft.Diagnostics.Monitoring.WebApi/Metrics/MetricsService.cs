// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    /// <summary>
    /// Periodically gets metrics from the app, and persists these to a metrics store.
    /// </summary>
    internal sealed class MetricsService : BackgroundService
    {
        private EventCounterPipeline _counterPipeline;
        private readonly IDiagnosticServices _services;
        private readonly MetricsStoreService _store;
        private IOptionsMonitor<MetricsOptions> _optionsMonitor;

        public MetricsService(IDiagnosticServices services,
            IOptionsMonitor<MetricsOptions> optionsMonitor,
            MetricsStoreService metricsStore)
        {
            _store = metricsStore;
            _services = services;
            _optionsMonitor = optionsMonitor;
        }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                stoppingToken.ThrowIfCancellationRequested();

                try
                {
                    IProcessInfo pi = await _services.GetProcessAsync(processKey: null, stoppingToken);
                    var client = new DiagnosticsClient(pi.EndpointInfo.Endpoint);

                    MetricsOptions options = _optionsMonitor.CurrentValue;
                    using var optionsTokenSource = new CancellationTokenSource();

                    //If metric options change, we need to cancel the existing metrics pipeline and restart with the new settings.
                    using IDisposable monitorListener = _optionsMonitor.OnChange((_, _) => optionsTokenSource.Cancel());

                    EventPipeCounterPipelineSettings counterSettings = EventCounterSettingsFactory.CreateSettings(options);

                    _counterPipeline = new EventCounterPipeline(client, counterSettings, loggers: new[] { new MetricsLogger(_store.MetricsStore) });

                    using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, optionsTokenSource.Token);
                    await _counterPipeline.RunAsync(linkedTokenSource.Token);
                }
                catch (Exception e) when (e is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
                {
                    //Most likely we failed to resolve the pid or metric configuration change. Attempt to do this again.
                    if (_counterPipeline != null)
                    {
                        await _counterPipeline.DisposeAsync();
                    }
                    await Task.Delay(5000);
                }
            }
        }

        public override async void Dispose()
        {
            base.Dispose();
            if (_counterPipeline != null)
            {
                await _counterPipeline.DisposeAsync();
            }
        }
    }
}
