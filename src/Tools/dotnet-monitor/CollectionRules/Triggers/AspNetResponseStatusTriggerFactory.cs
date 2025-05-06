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
using System.Linq;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Triggers
{
    /// <summary>
    /// Factory for creating a new AspNetResponseStatusTrigger trigger.
    /// </summary>
    internal sealed class AspNetResponseStatusTriggerFactory :
        ICollectionRuleTriggerFactory<AspNetResponseStatusOptions>
    {
        /// <inheritdoc/>
        private readonly EventPipeTriggerFactory _eventPipeTriggerFactory;
        private readonly ITraceEventTriggerFactory<AspNetRequestStatusTriggerSettings> _traceEventTriggerFactory;

        public AspNetResponseStatusTriggerFactory(
            EventPipeTriggerFactory eventPipeTriggerFactory,
            ITraceEventTriggerFactory<AspNetRequestStatusTriggerSettings> traceEventTriggerFactory)
        {
            _eventPipeTriggerFactory = eventPipeTriggerFactory;
            _traceEventTriggerFactory = traceEventTriggerFactory;
        }

        /// <inheritdoc/>
        public ICollectionRuleTrigger Create(IEndpointInfo endpointInfo, Action callback, AspNetResponseStatusOptions options)
        {
            var settings = new AspNetRequestStatusTriggerSettings
            {
                ExcludePaths = options.ExcludePaths,
                IncludePaths = options.IncludePaths,
                RequestCount = options.ResponseCount,
                StatusCodes = options.StatusCodes.Select(ParseRange).ToArray(),
                SlidingWindowDuration = options.SlidingWindowDuration ?? TimeSpan.Parse(AspNetResponseStatusOptionsDefaults.SlidingWindowDuration, CultureInfo.InvariantCulture),
            };

            var aspnetTriggerSourceConfiguration = new AspNetTriggerSourceConfiguration();

            return EventPipeTriggerFactory.Create(endpointInfo, aspnetTriggerSourceConfiguration, _traceEventTriggerFactory, settings, callback);
        }

        private static StatusCodeRange ParseRange(string range)
        {
            string[] ranges = range.Split('-');
            int min = int.Parse(ranges[0]);
            int max = min;
            if (ranges.Length == 2)
            {
                max = int.Parse(ranges[1]);
            }
            return new StatusCodeRange(min, max);
        }
    }

    internal sealed class AspNetResponseStatusTriggerDescriptor : ICollectionRuleTriggerDescriptor
    {
        public Type FactoryType => typeof(AspNetResponseStatusTriggerFactory);
        public Type? OptionsType => typeof(AspNetResponseStatusOptions);
        public string TriggerName => KnownCollectionRuleTriggers.AspNetResponseStatus;

        public bool TryBindOptions(IConfigurationSection settingsSection, out object? settings)
        {
            var options = new AspNetResponseStatusOptions();
            settingsSection.Bind_AspNetResponseStatusOptions(options);
            settings = options;
            return true;
        }
    }
}
