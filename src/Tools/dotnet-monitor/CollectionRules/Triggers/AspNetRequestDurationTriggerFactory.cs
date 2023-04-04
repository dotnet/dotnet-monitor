// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.EventPipe.Triggers;
using Microsoft.Diagnostics.Monitoring.EventPipe.Triggers.AspNet;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers;
using Microsoft.Extensions.Options;
using System;
using System.Globalization;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Triggers
{
    /// <summary>
    /// Factory for creating a new AspNetRequestDurationTrigger trigger.
    /// </summary>
    internal sealed class AspNetRequestDurationTriggerFactory :
        ICollectionRuleTriggerFactory<AspNetRequestDurationOptions>
    {
        private readonly EventPipeTriggerFactory _eventPipeTriggerFactory;
        private readonly ITraceEventTriggerFactory<AspNetRequestDurationTriggerSettings> _traceEventTriggerFactory;
        private readonly IOptionsMonitor<GlobalCounterOptions> _counterOptions;

        public AspNetRequestDurationTriggerFactory(
            EventPipeTriggerFactory eventPipeTriggerFactory,
            ITraceEventTriggerFactory<AspNetRequestDurationTriggerSettings> traceEventTriggerFactory,
            IOptionsMonitor<GlobalCounterOptions> counterOptions)
        {
            _eventPipeTriggerFactory = eventPipeTriggerFactory;
            _traceEventTriggerFactory = traceEventTriggerFactory;
            _counterOptions = counterOptions;
        }

        /// <inheritdoc/>
        public ICollectionRuleTrigger Create(IEndpointInfo endpointInfo, Action callback, AspNetRequestDurationOptions options)
        {
            var settings = new AspNetRequestDurationTriggerSettings
            {
                ExcludePaths = options.ExcludePaths,
                IncludePaths = options.IncludePaths,
                RequestCount = options.RequestCount,
                RequestDuration = options.RequestDuration ?? TimeSpan.Parse(AspNetRequestDurationOptionsDefaults.RequestDuration, CultureInfo.InvariantCulture),
                SlidingWindowDuration = options.SlidingWindowDuration ?? TimeSpan.Parse(AspNetRequestDurationOptionsDefaults.SlidingWindowDuration, CultureInfo.InvariantCulture),
            };

            //HACK we get the provider specific interval for the configuration
            var aspnetTriggerSourceConfiguration = new AspNetTriggerSourceConfiguration(_counterOptions.CurrentValue.GetProviderSpecificInterval(MonitoringSourceConfiguration.MicrosoftAspNetCoreHostingEventSourceName));

            return EventPipeTriggerFactory.Create(endpointInfo, aspnetTriggerSourceConfiguration, _traceEventTriggerFactory, settings, callback);
        }
    }
}
