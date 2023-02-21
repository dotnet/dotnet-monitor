// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.EventPipe.Triggers;
using Microsoft.Diagnostics.Monitoring.EventPipe.Triggers.SystemDiagnosticsMetrics;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers;
using Microsoft.Extensions.Options;
using System;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Triggers
{
    /// <summary>
    /// Factory for creating a new SystemDiagnosticsMetrics trigger.
    /// </summary>
    internal sealed class SystemDiagnosticsMetricsTriggerFactory :
        ICollectionRuleTriggerFactory<SystemDiagnosticsMetricsOptions>
    {
        private readonly EventPipeTriggerFactory _eventPipeTriggerFactory;
        private readonly ITraceEventTriggerFactory<SystemDiagnosticsMetricsTriggerSettings> _traceEventTriggerFactory;
        private readonly IOptionsMonitor<GlobalCounterOptions> _counterOptions;
        private IOptionsMonitor<MetricsOptions> _metricsOptions;

        public SystemDiagnosticsMetricsTriggerFactory(
            IOptionsMonitor<GlobalCounterOptions> counterOptions,
            IOptionsMonitor<MetricsOptions> metricsOptions,
            EventPipeTriggerFactory eventPipeTriggerFactory,
            ITraceEventTriggerFactory<SystemDiagnosticsMetricsTriggerSettings> traceEventTriggerFactory)
        {
            _eventPipeTriggerFactory = eventPipeTriggerFactory;
            _traceEventTriggerFactory = traceEventTriggerFactory;
            _counterOptions = counterOptions;
            _metricsOptions = metricsOptions;
        }

        /// <inheritdoc/>
        public ICollectionRuleTrigger Create(IEndpointInfo endpointInfo, Action callback, SystemDiagnosticsMetricsOptions options)
        {
            SystemDiagnosticsMetricsTriggerSettings settings = new()
            {
                ProviderName = options.ProviderName,
                CounterIntervalSeconds = _counterOptions.CurrentValue.GetIntervalSeconds(),
                InstrumentName = options.InstrumentName,
                GreaterThan = options.GreaterThan,
                LessThan = options.LessThan,
                HistogramPercentile = options.HistogramPercentile,
                SlidingWindowDuration = options.SlidingWindowDuration.GetValueOrDefault(TimeSpan.Parse(SystemDiagnosticsMetricsOptionsDefaults.SlidingWindowDuration)),
                MaxHistograms = _counterOptions.CurrentValue.GetMaxHistograms(),
                MaxTimeSeries = _counterOptions.CurrentValue.GetMaxTimeSeries(),
            };

            return EventPipeTriggerFactory.Create(
                endpointInfo,
                SystemDiagnosticsMetricsTrigger.CreateConfiguration(ref settings),
                _traceEventTriggerFactory,
                settings,
                callback);
        }
    }
}
