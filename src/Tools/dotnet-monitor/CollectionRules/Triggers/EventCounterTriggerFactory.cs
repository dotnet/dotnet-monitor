// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.EventPipe.Triggers;
using Microsoft.Diagnostics.Monitoring.EventPipe.Triggers.EventCounter;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers;
using System;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Triggers
{
    /// <summary>
    /// Factory for creating a new EventCounterTrigger trigger.
    /// </summary>
    internal sealed class EventCounterTriggerFactory :
        ICollectionRuleTriggerFactory<EventCounterOptions>
    {
        private readonly EventPipeTriggerFactory _eventPipeTriggerFactory;
        private readonly ITraceEventTriggerFactory<EventCounterTriggerSettings> _traceEventTriggerFactory;

        public EventCounterTriggerFactory(
            EventPipeTriggerFactory eventPipeTriggerFactory,
            ITraceEventTriggerFactory<EventCounterTriggerSettings> traceEventTriggerFactory)
        {
            _eventPipeTriggerFactory = eventPipeTriggerFactory;
            _traceEventTriggerFactory = traceEventTriggerFactory;
        }

        /// <inheritdoc/>
        public ICollectionRuleTrigger Create(IProcessInfo processInfo, Action callback, EventCounterOptions options)
        {
            EventCounterTriggerSettings settings = new()
            {
                ProviderName = options.ProviderName,
                CounterIntervalSeconds = options.Frequency.GetValueOrDefault(EventCounterOptionsDefaults.Frequency),
                CounterName = options.CounterName,
                GreaterThan = options.GreaterThan,
                LessThan = options.LessThan,
                SlidingWindowDuration = options.SlidingWindowDuration.GetValueOrDefault(TimeSpan.Parse(EventCounterOptionsDefaults.SlidingWindowDuration))
            };

            return _eventPipeTriggerFactory.Create(
                processInfo,
                EventCounterTrigger.CreateConfiguration(settings),
                _traceEventTriggerFactory,
                settings,
                callback);
        }
    }
}
