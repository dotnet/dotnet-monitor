// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.EventPipe.Triggers;
using Microsoft.Diagnostics.Monitoring.EventPipe.Triggers.EventCounter;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers.EventCounterShortcuts;
using Microsoft.Extensions.Options;
using System;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Triggers
{
    /// <summary>
    /// Factory for creating a new EventCounterTrigger trigger.
    /// </summary>
    internal sealed class EventCounterTriggerFactory :
        ICollectionRuleTriggerFactory<EventCounterOptions>,
        ICollectionRuleTriggerFactory<CPUUsageOptions>,
        ICollectionRuleTriggerFactory<GCHeapSizeOptions>,
        ICollectionRuleTriggerFactory<ThreadpoolQueueLengthOptions>
    {
        private readonly EventPipeTriggerFactory _eventPipeTriggerFactory;
        private readonly ITraceEventTriggerFactory<EventCounterTriggerSettings> _traceEventTriggerFactory;
        private readonly IOptionsMonitor<GlobalCounterOptions> _counterOptions;

        public EventCounterTriggerFactory(
            IOptionsMonitor<GlobalCounterOptions> counterOptions,
            EventPipeTriggerFactory eventPipeTriggerFactory,
            ITraceEventTriggerFactory<EventCounterTriggerSettings> traceEventTriggerFactory)
        {
            _eventPipeTriggerFactory = eventPipeTriggerFactory;
            _traceEventTriggerFactory = traceEventTriggerFactory;
            _counterOptions = counterOptions;
        }

        /// <inheritdoc/>
        public ICollectionRuleTrigger Create(IEndpointInfo endpointInfo, Action callback, EventCounterOptions options)
        {
            EventCounterTriggerSettings settings = new()
            {
                ProviderName = options.ProviderName,
                CounterIntervalSeconds = _counterOptions.CurrentValue.GetProviderSpecificInterval(options.ProviderName),
                CounterName = options.CounterName,
                GreaterThan = options.GreaterThan,
                LessThan = options.LessThan,
                SlidingWindowDuration = options.SlidingWindowDuration.GetValueOrDefault(TimeSpan.Parse(EventCounterOptionsDefaults.SlidingWindowDuration))
            };

            return EventPipeTriggerFactory.Create(
                endpointInfo,
                EventCounterTrigger.CreateConfiguration(settings),
                _traceEventTriggerFactory,
                settings,
                callback);
        }

        public ICollectionRuleTrigger Create(IEndpointInfo endpointInfo, Action callback, CPUUsageOptions options)
        {
            EventCounterOptions eventCounterOptions = ToEventCounterOptions(options);
            return Create(endpointInfo, callback, eventCounterOptions);
        }

        internal static EventCounterOptions ToEventCounterOptions(CPUUsageOptions options)
        {
            return ToEventCounterOptions(options, KnownEventCounterConstants.SystemRuntime, KnownEventCounterConstants.CPUUsage, greaterThanDefault: CPUUsageOptionsDefaults.GreaterThan);
        }

        public ICollectionRuleTrigger Create(IEndpointInfo endpointInfo, Action callback, GCHeapSizeOptions options)
        {
            EventCounterOptions eventCounterOptions = ToEventCounterOptions(options);
            return Create(endpointInfo, callback, eventCounterOptions);
        }

        internal static EventCounterOptions ToEventCounterOptions(GCHeapSizeOptions options)
        {
            return ToEventCounterOptions(options, KnownEventCounterConstants.SystemRuntime, KnownEventCounterConstants.GCHeapSize, greaterThanDefault: GCHeapSizeOptionsDefaults.GreaterThan);
        }

        public ICollectionRuleTrigger Create(IEndpointInfo endpointInfo, Action callback, ThreadpoolQueueLengthOptions options)
        {
            EventCounterOptions eventCounterOptions = ToEventCounterOptions(options);
            return Create(endpointInfo, callback, eventCounterOptions);
        }

        internal static EventCounterOptions ToEventCounterOptions(ThreadpoolQueueLengthOptions options)
        {
            return ToEventCounterOptions(options, KnownEventCounterConstants.SystemRuntime, KnownEventCounterConstants.ThreadpoolQueueLength, greaterThanDefault: ThreadpoolQueueLengthOptionsDefaults.GreaterThan);
        }

        /// <summary>
        /// Intermediary translation layer is used because it allows for testing whether built-in triggers correctly map
        /// from IEventCounterShortcuts into EventCounterOptions.
        /// </summary> 
        internal static EventCounterOptions ToEventCounterOptions(IEventCounterShortcuts options,
            string providerName,
            string counterName,
            double? greaterThanDefault = null,
            double? lessThanDefault = null,
            TimeSpan? slidingWindowDurationDefault = null)
        {
            return new EventCounterOptions()
            {
                ProviderName = providerName,
                CounterName = counterName,
                GreaterThan = options.LessThan.HasValue ? options.GreaterThan : (options.GreaterThan ?? greaterThanDefault),
                LessThan = options.GreaterThan.HasValue ? options.LessThan : (options.LessThan ?? lessThanDefault),
                SlidingWindowDuration = options.SlidingWindowDuration ?? slidingWindowDurationDefault
            };
        }
    }
}
