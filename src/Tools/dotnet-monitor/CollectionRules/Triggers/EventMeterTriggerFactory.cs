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
    /// Factory for creating a new EventMeter trigger.
    /// </summary>
    internal sealed class EventMeterTriggerFactory :
        ICollectionRuleTriggerFactory<EventMeterOptions>
    {
        private readonly EventPipeTriggerFactory _eventPipeTriggerFactory;
        private readonly ITraceEventTriggerFactory<SystemDiagnosticsMetricsTriggerSettings> _traceEventTriggerFactory;
        private readonly IOptionsMonitor<GlobalCounterOptions> _counterOptions;

        public EventMeterTriggerFactory(
            IOptionsMonitor<GlobalCounterOptions> counterOptions,
            EventPipeTriggerFactory eventPipeTriggerFactory,
            ITraceEventTriggerFactory<SystemDiagnosticsMetricsTriggerSettings> traceEventTriggerFactory)
        {
            _eventPipeTriggerFactory = eventPipeTriggerFactory;
            _traceEventTriggerFactory = traceEventTriggerFactory;
            _counterOptions = counterOptions;
        }

        /// <inheritdoc/>
        public ICollectionRuleTrigger Create(IEndpointInfo endpointInfo, Action callback, EventMeterOptions options)
        {
            SystemDiagnosticsMetricsTriggerSettings settings = new()
            {
                MeterName = options.MeterName,
                CounterIntervalSeconds = _counterOptions.CurrentValue.GetIntervalSeconds(),
                InstrumentName = options.InstrumentName,
                GreaterThan = options.GreaterThan,
                LessThan = options.LessThan,
                HistogramPercentile = options.HistogramPercentile,
                SlidingWindowDuration = options.SlidingWindowDuration.GetValueOrDefault(TimeSpan.Parse(EventMeterOptionsDefaults.SlidingWindowDuration)),
                MaxHistograms = _counterOptions.CurrentValue.GetMaxHistograms(),
                MaxTimeSeries = _counterOptions.CurrentValue.GetMaxTimeSeries(),
                UseSharedSession = endpointInfo.RuntimeVersion?.Major >= 8
            };

            return EventPipeTriggerFactory.Create(
                endpointInfo,
                SystemDiagnosticsMetricsTrigger.CreateConfiguration(settings),
                _traceEventTriggerFactory,
                settings,
                callback);
        }
    }
}
