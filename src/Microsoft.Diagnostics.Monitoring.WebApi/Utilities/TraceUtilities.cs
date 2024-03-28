// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.WebApi.Validation;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal sealed class GcCollectConfiguration : MonitoringSourceConfiguration
    {
        public GcCollectConfiguration()
        {
            RequestRundown = false;
        }

        public override IList<EventPipeProvider> GetProviders() =>            
            new EventPipeProvider[] {
                    new EventPipeProvider(
                        name: "Microsoft-Windows-DotNETRuntime",
                        eventLevel: System.Diagnostics.Tracing.EventLevel.Informational,
                        // keywords: (long)ClrTraceEventParser.Keywords.GC
                        keywords: 1
                    ),
                    new EventPipeProvider(
                        name: "Microsoft-Windows-DotNETRuntimePrivate",
                        eventLevel: System.Diagnostics.Tracing.EventLevel.Informational,
                        // keywords: (long)ClrTraceEventParser.Keywords.GC
                        keywords: 1
                    )
            };
    }

    internal static class TraceUtilities
    {
        public static MonitoringSourceConfiguration GetTraceConfiguration(Models.TraceProfile profile, GlobalCounterOptions options)
        {
            var configurations = new List<MonitoringSourceConfiguration>();
            if (profile.HasFlag(Models.TraceProfile.Cpu))
            {
                configurations.Add(new CpuProfileConfiguration());
            }
            if (profile.HasFlag(Models.TraceProfile.Http))
            {
                configurations.Add(new HttpRequestSourceConfiguration());
            }
            if (profile.HasFlag(Models.TraceProfile.Logs))
            {
                configurations.Add(new LoggingSourceConfiguration(
                    LogLevel.Trace,
                    LogMessageType.FormattedMessage | LogMessageType.JsonMessage,
                    filterSpecs: null,
                    useAppFilters: true,
                    collectScopes: true));
            }
            if (profile.HasFlag(Models.TraceProfile.Metrics))
            {
                IEnumerable<MetricEventPipeProvider> defaultProviders = MonitoringSourceConfiguration.DefaultMetricProviders.Select(provider => new MetricEventPipeProvider
                {
                    Provider = provider,
                    IntervalSeconds = options.GetProviderSpecificInterval(provider),
                    Type = MetricType.EventCounter
                });

                configurations.Add(new MetricSourceConfiguration(options.GetIntervalSeconds(), defaultProviders));
            }
            if (profile.HasFlag(Models.TraceProfile.GcCollect))
            {
                configurations.Add(new GcCollectConfiguration());
            }

            return new AggregateSourceConfiguration(configurations.ToArray());
        }

        public static MonitoringSourceConfiguration GetTraceConfiguration(Models.EventPipeProvider[] configurationProviders, bool requestRundown, int bufferSizeInMB)
        {
            var providers = new List<EventPipeProvider>();

            foreach (Models.EventPipeProvider providerModel in configurationProviders)
            {
                if (!IntegerOrHexStringAttribute.TryParse(providerModel.Keywords, out long keywords, out string parseError))
                {
                    throw new InvalidOperationException(parseError);
                }

                providers.Add(new EventPipeProvider(
                    providerModel.Name,
                    providerModel.EventLevel,
                    keywords,
                    providerModel.Arguments
                    ));
            }

            return new EventPipeProviderSourceConfiguration(
                providers: providers.ToArray(),
                requestRundown: requestRundown,
                bufferSizeInMB: bufferSizeInMB);
        }
    }
}
