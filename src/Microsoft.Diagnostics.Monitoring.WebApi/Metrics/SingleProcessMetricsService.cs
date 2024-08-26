// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal sealed class SingleProcessMetricsService : IAsyncDisposable
    {
        private readonly IOptionsMonitor<MetricsOptions> _metricsOptions;
        private readonly GlobalCounterOptions _counterOptions;
        private readonly IProcessInfo _process;
        private readonly MetricsStore _metricsStore;

        private MetricsPipeline? _counterPipeline;

        public int ProcessId => _process.EndpointInfo.ProcessId;

        public SingleProcessMetricsService(IOptionsMonitor<MetricsOptions> metricsOptions,
            GlobalCounterOptions counterOptions, IProcessInfo process, MetricsStore metricsStore)
        {
            _metricsOptions = metricsOptions;
            _counterOptions = counterOptions;
            _process = process;
            _metricsStore = metricsStore;
        }

        public async Task<SingleProcessMetricsService> StartMetricsPipelineForProcessAsync(CancellationToken stoppingToken)
        {
            var client = new DiagnosticsClient(_process.EndpointInfo.Endpoint);

            using var optionsTokenSource = new CancellationTokenSource();

            //If metric options change, we need to cancel the existing metrics pipeline and restart with the new settings.
            using IDisposable? monitorListener = _metricsOptions.OnChange((_, _) => optionsTokenSource.SafeCancel());

            MetricsPipelineSettings counterSettings = MetricsSettingsFactory.CreateSettings(_counterOptions, Timeout.Infinite, _metricsOptions.CurrentValue);
            counterSettings.UseSharedSession = _process.EndpointInfo.RuntimeVersion?.Major >= 8;

            await using var counterPipeline = new MetricsPipeline(client, counterSettings, loggers: new[] { new MetricsLogger(_metricsStore) });
            _counterPipeline = counterPipeline;

            try
            {
                using var linkedTokenSource =
                    CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, optionsTokenSource.Token);
                await counterPipeline.RunAsync(linkedTokenSource.Token);
            }
            catch
            {
                //ignore - if pipeline was broken, caller will handle reconnect
            }

            return this;
        }

        public ValueTask DisposeAsync()
            => _counterPipeline?.DisposeAsync() ?? ValueTask.CompletedTask;
    }
}
