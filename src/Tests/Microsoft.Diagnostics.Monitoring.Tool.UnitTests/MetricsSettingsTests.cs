// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    public class MetricsSettingsTests
    {
        private readonly ITestOutputHelper _outputHelper;

        public MetricsSettingsTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Fact]
        public void ValidateDefaultMetricSettings()
        {
            const int ExpectedGlobalInterval = 5;
            int customInterval = ExpectedGlobalInterval + 1;
            int[] expectedIntervals = MonitoringSourceConfiguration.DefaultMetricProviders.Select((_, index) => index + ExpectedGlobalInterval + 1).ToArray();

            using IHost host = TestHostHelper.CreateHost(_outputHelper, (rootOptions) =>
            {
                rootOptions.AddGlobalCounter(ExpectedGlobalInterval);
                foreach (string provider in MonitoringSourceConfiguration.DefaultMetricProviders)
                {
                    rootOptions.AddProviderInterval(provider, customInterval++);
                }
            },
            servicesCallback: null,
            loggingCallback: null,
            overrideSource: null);

            var options = host.Services.GetRequiredService<IOptionsMonitor<GlobalCounterOptions>>();

            var settings = MetricsSettingsFactory.CreateSettings(options.CurrentValue, includeDefaults: true, durationSeconds: 30);

            Assert.Equal(ExpectedGlobalInterval, settings.CounterIntervalSeconds);

            customInterval = ExpectedGlobalInterval + 1;
            foreach (string provider in MonitoringSourceConfiguration.DefaultMetricProviders)
            {
                Assert.Equal(customInterval++, GetInterval(settings, provider));
            }
        }

        [Fact]
        public void ValidateApiMetricsSettings()
        {
            const int ExpectedGlobalInterval = 5;
            const int CustomInterval = 6;
            const string CustomProvider1 = nameof(CustomProvider1);
            const string CustomProvider2 = nameof(CustomProvider2);

            using IHost host = TestHostHelper.CreateHost(_outputHelper, (rootOptions) =>
            {
                rootOptions.AddGlobalCounter(ExpectedGlobalInterval)
                .AddProviderInterval(CustomProvider1, CustomInterval);
            },
            servicesCallback: null,
            loggingCallback: null,
            overrideSource: null);

            var options = host.Services.GetRequiredService<IOptionsMonitor<GlobalCounterOptions>>();

            var settings = MetricsSettingsFactory.CreateSettings(options.CurrentValue, 30, new WebApi.Models.EventMetricsConfiguration
            {
                IncludeDefaultProviders = false,
                Providers = new[] { new WebApi.Models.EventMetricsProvider { ProviderName = CustomProvider1 }, new WebApi.Models.EventMetricsProvider { ProviderName = CustomProvider2 } }
            });

            Assert.Equal(ExpectedGlobalInterval, settings.CounterIntervalSeconds);
            Assert.Equal(CustomInterval, GetInterval(settings, CustomProvider1));
            Assert.Null(GetInterval(settings, CustomProvider2));
        }

        [Fact]
        public void ValidateMetricStoreSettings()
        {
            const int ExpectedGlobalInterval = 5;
            const int CustomInterval = 6;
            const string CustomProvider1 = nameof(CustomProvider1);
            const string CustomProvider2 = nameof(CustomProvider2);

            using IHost host = TestHostHelper.CreateHost(_outputHelper, (rootOptions) =>
            {
                rootOptions.AddGlobalCounter(ExpectedGlobalInterval)
                .AddProviderInterval(CustomProvider1, CustomInterval);
            },
            servicesCallback: null,
            loggingCallback: null,
            overrideSource: null);

            var options = host.Services.GetRequiredService<IOptionsMonitor<GlobalCounterOptions>>();

            var settings = MetricsSettingsFactory.CreateSettings(options.CurrentValue, new MetricsOptions
            {
                IncludeDefaultProviders = false,
                Providers = new List<MetricProvider> { new MetricProvider { ProviderName = CustomProvider1 }, new MetricProvider { ProviderName = CustomProvider2 } }
            });

            Assert.Equal(ExpectedGlobalInterval, settings.CounterIntervalSeconds);
            Assert.Equal(CustomInterval, GetInterval(settings, CustomProvider1));
            Assert.Null(GetInterval(settings, CustomProvider2));
        }

        private static float? GetInterval(MetricsPipelineSettings settings, string provider)
        {
            EventPipeCounterGroup counterGroup = settings.CounterGroups.FirstOrDefault(g => g.ProviderName == provider);
            Assert.NotNull(counterGroup);
            return counterGroup.IntervalSeconds;
        }
    }
}
