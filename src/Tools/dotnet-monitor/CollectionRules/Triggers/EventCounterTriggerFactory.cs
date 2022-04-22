﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                CounterIntervalSeconds = _counterOptions.CurrentValue.GetIntervalSeconds(),
                CounterName = options.CounterName,
                GreaterThan = options.GreaterThan,
                LessThan = options.LessThan,
                SlidingWindowDuration = options.SlidingWindowDuration.GetValueOrDefault(TimeSpan.Parse(EventCounterOptionsDefaults.SlidingWindowDuration))
            };

            return _eventPipeTriggerFactory.Create(
                endpointInfo,
                EventCounterTrigger.CreateConfiguration(settings),
                _traceEventTriggerFactory,
                settings,
                callback);
        }

        public ICollectionRuleTrigger Create(IEndpointInfo endpointInfo, Action callback, CPUUsageOptions options)
        {
            return Create(endpointInfo, callback, options, IEventCounterShortcutsConstants.SystemRuntime, IEventCounterShortcutsConstants.CPUUsage, greaterThanDefault: CPUUsageOptionsDefaults.GreaterThan);
        }

        public ICollectionRuleTrigger Create(IEndpointInfo endpointInfo, Action callback, GCHeapSizeOptions options)
        {
            return Create(endpointInfo, callback, options, IEventCounterShortcutsConstants.SystemRuntime, IEventCounterShortcutsConstants.GCHeapSize, greaterThanDefault:GCHeapSizeOptionsDefaults.GreaterThan);
        }

        public ICollectionRuleTrigger Create(IEndpointInfo endpointInfo, Action callback, ThreadpoolQueueLengthOptions options)
        {
            return Create(endpointInfo,callback, options, IEventCounterShortcutsConstants.SystemRuntime, IEventCounterShortcutsConstants.ThreadpoolQueueLength, greaterThanDefault: ThreadpoolQueueLengthOptionsDefaults.GreaterThan);
        }

        private ICollectionRuleTrigger Create(IEndpointInfo endpointInfo,
            Action callback,
            IEventCounterShortcuts options,
            string providerName,
            string counterName,
            double? greaterThanDefault = null,
            double? lessThanDefault = null,
            TimeSpan? slidingWindowDurationDefault = null)
        {
            return Create(endpointInfo, callback, new EventCounterOptions()
            {
                ProviderName = providerName,
                CounterName = counterName,
                GreaterThan = options.LessThan.HasValue ? options.GreaterThan : (options.GreaterThan ?? greaterThanDefault),
                LessThan = options.GreaterThan.HasValue ? options.LessThan : (options.LessThan ?? lessThanDefault),
                SlidingWindowDuration = options.SlidingWindowDuration ?? slidingWindowDurationDefault
            });
        }
    }
}
