// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace CollectionRuleActions.UnitTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    [Collection(TestCollections.CollectionRuleActions)]
    public class CollectLiveMetricsActionTests
    {
        private readonly ITestOutputHelper _outputHelper;
        private readonly EndpointUtilities _endpointUtilities;

        private const string DefaultRuleName = "LiveMetricsTestRule";
        private const int IntervalSeconds = 2;

        public CollectLiveMetricsActionTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
            _endpointUtilities = new(_outputHelper);
        }

        [Theory]
        [MemberData(nameof(ActionTestsHelper.GetTfms), MemberType = typeof(ActionTestsHelper))]
        public Task CollectLiveMetricsAction_CustomProviders(TargetFrameworkMoniker tfm) =>
            CollectLiveMetricsAction_CustomProvidersCore(tfm);

        [Fact]
        public Task CollectLiveMetricsAction_CustomArtifactName() =>
            CollectLiveMetricsAction_CustomProvidersCore(TargetFrameworkMoniker.Current, artifactName: Guid.NewGuid().ToString("n"));

        private async Task CollectLiveMetricsAction_CustomProvidersCore(TargetFrameworkMoniker tfm, string artifactName = null)
        {
            using TemporaryDirectory tempDirectory = new(_outputHelper);

            const string providerName = MonitoringSourceConfiguration.SystemRuntimeEventSourceName;

            var counterNames = new[] { "cpu-usage", "working-set" };

            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                rootOptions.AddGlobalCounter(IntervalSeconds);

                rootOptions.AddFileSystemEgress(ActionTestsConstants.ExpectedEgressProvider, tempDirectory.FullName);

                rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddCollectLiveMetricsAction(ActionTestsConstants.ExpectedEgressProvider, options =>
                    {
                        options.ArtifactName = artifactName;
                        options.Duration = TimeSpan.FromSeconds(CommonTestTimeouts.LiveMetricsDurationSeconds);
                        options.IncludeDefaultProviders = false;
                        options.Providers =
                        [
                            new EventMetricsProvider
                            {
                                ProviderName = providerName,
                                CounterNames = counterNames,
                            }
                        ];
                    })
                    .SetStartupTrigger();
            }, async host =>
            {
                CollectLiveMetricsOptions options = ActionTestsHelper.GetActionOptions<CollectLiveMetricsOptions>(host, DefaultRuleName);

                ICollectionRuleActionFactoryProxy factory;
                Assert.True(host.Services.GetService<ICollectionRuleActionOperations>().TryCreateFactory(KnownCollectionRuleActions.CollectLiveMetrics, out factory));

                EndpointInfoSourceCallback callback = new(_outputHelper);
                await using ServerSourceHolder sourceHolder = await _endpointUtilities.StartServerAsync(callback);

                await using AppRunner runner = _endpointUtilities.CreateAppRunner(Assembly.GetExecutingAssembly(), sourceHolder.TransportName, tfm);

                Task<IProcessInfo> processInfoTask = callback.WaitAddedProcessInfoAsync(runner, CommonTestTimeouts.StartProcess);

                await runner.ExecuteAsync(async () =>
                {
                    IProcessInfo processInfo = await processInfoTask;

                    ICollectionRuleAction action = factory.Create(processInfo, options);

                    CollectionRuleActionResult result = await ActionTestsHelper.ExecuteAndDisposeAsync(action, CommonTestTimeouts.LiveMetricsTimeout);

                    string egressPath = ActionTestsHelper.ValidateEgressPath(result, artifactName);

                    using FileStream liveMetricsStream = new(egressPath, FileMode.Open, FileAccess.Read);
                    Assert.NotNull(liveMetricsStream);

                    var metrics = LiveMetricsTestUtilities.GetAllMetrics(liveMetricsStream);

                    await LiveMetricsTestUtilities.ValidateMetrics(new[] { providerName },
                        counterNames,
                        metrics,
                        strict: true);

                    await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                });
            });
        }

        [Theory(Skip = "Flaky")]
        [MemberData(nameof(ActionTestsHelper.GetTfms), MemberType = typeof(ActionTestsHelper))]
        public async Task CollectLiveMetricsAction_CustomMeters(TargetFrameworkMoniker tfm)
        {
            using TemporaryDirectory tempDirectory = new(_outputHelper);

            const string ExpectedMeterName = LiveMetricsTestConstants.ProviderName1;

            var ExpectedInstrumentNames = new[] { LiveMetricsTestConstants.GaugeName, LiveMetricsTestConstants.HistogramName2 };

            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                rootOptions.AddGlobalCounter(IntervalSeconds);

                rootOptions.AddFileSystemEgress(ActionTestsConstants.ExpectedEgressProvider, tempDirectory.FullName);

                rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddCollectLiveMetricsAction(ActionTestsConstants.ExpectedEgressProvider, options =>
                    {
                        options.Duration = TimeSpan.FromSeconds(CommonTestTimeouts.LiveMetricsDurationSeconds);
                        options.IncludeDefaultProviders = false;
                        options.Meters = new[]
                        {
                            new EventMetricsMeter
                            {
                                MeterName = ExpectedMeterName,
                                InstrumentNames = ExpectedInstrumentNames,
                            }
                        };
                    })
                    .SetStartupTrigger();
            }, async host =>
            {
                CollectLiveMetricsOptions options = ActionTestsHelper.GetActionOptions<CollectLiveMetricsOptions>(host, DefaultRuleName);

                ICollectionRuleActionFactoryProxy factory;
                Assert.True(host.Services.GetService<ICollectionRuleActionOperations>().TryCreateFactory(KnownCollectionRuleActions.CollectLiveMetrics, out factory));

                EndpointInfoSourceCallback callback = new(_outputHelper);
                await using ServerSourceHolder sourceHolder = await _endpointUtilities.StartServerAsync(callback);

                await using AppRunner runner = _endpointUtilities.CreateAppRunner(Assembly.GetExecutingAssembly(), sourceHolder.TransportName, tfm);
                runner.ScenarioName = TestAppScenarios.Metrics.Name;

                Task<IProcessInfo> processInfoTask = callback.WaitAddedProcessInfoAsync(runner, CommonTestTimeouts.StartProcess);

                await runner.ExecuteAsync(async () =>
                {
                    IProcessInfo processInfo = await processInfoTask;

                    ICollectionRuleAction action = factory.Create(processInfo, options);

                    CollectionRuleActionResult result = await ActionTestsHelper.ExecuteAndDisposeAsync(action, CommonTestTimeouts.LiveMetricsTimeout);

                    string egressPath = ActionTestsHelper.ValidateEgressPath(result);

                    using FileStream liveMetricsStream = new(egressPath, FileMode.Open, FileAccess.Read);
                    Assert.NotNull(liveMetricsStream);

                    var metrics = LiveMetricsTestUtilities.GetAllMetrics(liveMetricsStream);

                    await LiveMetricsTestUtilities.ValidateMetrics(new[] { ExpectedMeterName },
                        ExpectedInstrumentNames,
                        metrics,
                        strict: true);

                    await runner.SendCommandAsync(TestAppScenarios.Metrics.Commands.Continue);
                });
            });
        }
    }
}
