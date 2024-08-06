// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    /// <summary>
    /// Utility class to create metric settings (for both configuration and on demand metrics).
    /// </summary>
    internal static class MetricsSettingsFactory
    {
        public static MetricsPipelineSettings CreateSettings(GlobalCounterOptions counterOptions, bool includeDefaults,
            int durationSeconds)
        {
            return CreateSettings(includeDefaults,
                durationSeconds,
                counterOptions.GetIntervalSeconds(),
                counterOptions.Providers,
                counterOptions.GetMaxHistograms(),
                counterOptions.GetMaxTimeSeries(),
                () => new List<EventPipeCounterGroup>(0));
        }

        public static MetricsPipelineSettings CreateSettings(GlobalCounterOptions counterOptions, int durationSeconds,
            MetricsOptions options)
        {
            return CreateSettings(
                options.IncludeDefaultProviders.GetValueOrDefault(MetricsOptionsDefaults.IncludeDefaultProviders),
                durationSeconds,
                counterOptions.GetIntervalSeconds(),
                counterOptions.Providers,
                counterOptions.GetMaxHistograms(),
                counterOptions.GetMaxTimeSeries(),
                () => ConvertCounterGroups(options.Providers, options.Meters));
        }

        public static MetricsPipelineSettings CreateSettings(GlobalCounterOptions counterOptions, int durationSeconds,
            Models.EventMetricsConfiguration configuration)
        {
            return CreateSettings(configuration.IncludeDefaultProviders,
                durationSeconds,
                counterOptions.GetIntervalSeconds(),
                counterOptions.Providers,
                counterOptions.GetMaxHistograms(),
                counterOptions.GetMaxTimeSeries(),
                () => ConvertCounterGroups(configuration.Providers, configuration.Meters));
        }

        private static MetricsPipelineSettings CreateSettings(bool includeDefaults,
            int durationSeconds,
            float counterInterval,
            IDictionary<string, GlobalProviderOptions>? intervalMap,
            int maxHistograms,
            int maxTimeSeries,
            Func<List<EventPipeCounterGroup>> createCounterGroups)
        {
            List<EventPipeCounterGroup> eventPipeCounterGroups = createCounterGroups();

            if (includeDefaults)
            {
                eventPipeCounterGroups.Add(new EventPipeCounterGroup { ProviderName = MonitoringSourceConfiguration.SystemRuntimeEventSourceName, Type = CounterGroupType.EventCounter });
                eventPipeCounterGroups.Add(new EventPipeCounterGroup { ProviderName = MonitoringSourceConfiguration.MicrosoftAspNetCoreHostingEventSourceName, Type = CounterGroupType.EventCounter });
                eventPipeCounterGroups.Add(new EventPipeCounterGroup { ProviderName = MonitoringSourceConfiguration.GrpcAspNetCoreServer, Type = CounterGroupType.EventCounter });
            }

            if (intervalMap != null)
            {
                foreach (EventPipeCounterGroup counterGroup in eventPipeCounterGroups)
                {
                    if (intervalMap.TryGetValue(counterGroup.ProviderName, out GlobalProviderOptions? providerInterval))
                    {
                        Debug.Assert(counterGroup.IntervalSeconds == null, "Unexpected value for provider interval");
                        counterGroup.IntervalSeconds = providerInterval.IntervalSeconds;
                    }
                }
            }

            return new MetricsPipelineSettings
            {
                CounterGroups = eventPipeCounterGroups.ToArray(),
                Duration = Utilities.ConvertSecondsToTimeSpan(durationSeconds),
                CounterIntervalSeconds = counterInterval,
                MaxHistograms = maxHistograms,
                MaxTimeSeries = maxTimeSeries
            };
        }

        private static List<EventPipeCounterGroup> ConvertCounterGroups(IList<MetricProvider>? providers, IList<MeterConfiguration>? meters)
        {
            List<EventPipeCounterGroup> counterGroups = new();

            if (providers?.Count > 0)
            {
                foreach (MetricProvider customProvider in providers)
                {
                    var customCounterGroup = new EventPipeCounterGroup { ProviderName = customProvider.ProviderName, Type = CounterGroupType.EventCounter };
                    if (customProvider.CounterNames?.Count > 0)
                    {
                        customCounterGroup.CounterNames = customProvider.CounterNames.ToArray();
                    }
                    counterGroups.Add(customCounterGroup);
                }
            }

            if (meters?.Count > 0)
            {
                foreach (MeterConfiguration meter in meters)
                {
                    var customCounterGroup = new EventPipeCounterGroup { ProviderName = meter.MeterName, Type = CounterGroupType.Meter };
                    if (meter.InstrumentNames?.Count > 0)
                    {
                        customCounterGroup.CounterNames = meter.InstrumentNames.ToArray();
                    }
                    counterGroups.Add(customCounterGroup);
                }
            }

            return counterGroups;
        }

        private static List<EventPipeCounterGroup> ConvertCounterGroups(IList<Models.EventMetricsProvider>? providers, IList<Models.EventMetricsMeter>? meters)
        {
            List<EventPipeCounterGroup> counterGroups = new();

            if (providers?.Count > 0)
            {
                foreach (Models.EventMetricsProvider customProvider in providers)
                {
                    EventPipeCounterGroup customCounterGroup = new() { ProviderName = customProvider.ProviderName, Type = CounterGroupType.EventCounter };
                    if (customProvider.CounterNames?.Length > 0)
                    {
                        customCounterGroup.CounterNames = customProvider.CounterNames.ToArray();
                    }

                    counterGroups.Add(customCounterGroup);
                }
            }

            if (meters?.Count > 0)
            {
                foreach (Models.EventMetricsMeter customMeter in meters)
                {
                    EventPipeCounterGroup customCounterGroup = new() { ProviderName = customMeter.MeterName, Type = CounterGroupType.Meter };
                    if (customMeter.InstrumentNames?.Length > 0)
                    {
                        customCounterGroup.CounterNames = customMeter.InstrumentNames.ToArray();
                    }

                    counterGroups.Add(customCounterGroup);
                }
            }

            return counterGroups;
        }
    }
}
