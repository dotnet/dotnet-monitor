// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.EventPipe.Triggers;
using Microsoft.Diagnostics.Monitoring.EventPipe.Triggers.AspNet;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Binder.SourceGeneration;
using System;
using System.Globalization;
using Microsoft.Extensions.Options;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Triggers
{
    /// <summary>
    /// Factory for creating a new AspNetRequestCount trigger.
    /// </summary>
    internal sealed class AspNetRequestCountTriggerFactory :
        ICollectionRuleTriggerFactory<AspNetRequestCountOptions>
    {
        private readonly EventPipeTriggerFactory _eventPipeTriggerFactory;
        private readonly ITraceEventTriggerFactory<AspNetRequestCountTriggerSettings> _traceEventTriggerFactory;

        public AspNetRequestCountTriggerFactory(
            EventPipeTriggerFactory eventPipeTriggerFactory,
            ITraceEventTriggerFactory<AspNetRequestCountTriggerSettings> traceEventTriggerFactory)
        {
            _eventPipeTriggerFactory = eventPipeTriggerFactory;
            _traceEventTriggerFactory = traceEventTriggerFactory;
        }

        /// <inheritdoc/>
        public ICollectionRuleTrigger Create(IEndpointInfo endpointInfo, Action callback, AspNetRequestCountOptions options)
        {
            var settings = new AspNetRequestCountTriggerSettings
            {
                ExcludePaths = options.ExcludePaths,
                IncludePaths = options.IncludePaths,
                RequestCount = options.RequestCount,
                SlidingWindowDuration = options.SlidingWindowDuration ?? TimeSpan.Parse(AspNetRequestCountOptionsDefaults.SlidingWindowDuration, CultureInfo.InvariantCulture),
            };

            var aspnetTriggerSourceConfiguration = new AspNetTriggerSourceConfiguration();

            return EventPipeTriggerFactory.Create(endpointInfo, aspnetTriggerSourceConfiguration, _traceEventTriggerFactory, settings, callback);
        }
    }

    [OptionsValidator]
    internal sealed partial class AspNetRequestCountTriggerDescriptor : ICollectionRuleTriggerDescriptor<AspNetRequestCountOptions, AspNetRequestCountTriggerFactory>
    {
        public string TriggerName => KnownCollectionRuleTriggers.AspNetRequestCount;

        public bool TryBindOptions(IConfigurationSection settingsSection, out AspNetRequestCountOptions options)
        {
            options = new AspNetRequestCountOptions();
            settingsSection.Bind_AspNetRequestCountOptions(options);
            return true;
        }
    }
}
