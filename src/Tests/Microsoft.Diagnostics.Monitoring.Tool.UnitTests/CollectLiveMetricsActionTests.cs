// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    public class CollectLiveMetricsActionTests
    {
        private readonly ITestOutputHelper _outputHelper;
        private readonly EndpointUtilities _endpointUtilities;

        private const string DefaultRuleName = "LiveMetricsTestRule";
        private const int IntervalSeconds = 2;
        private const int DurationSeconds = IntervalSeconds + 1;

        public CollectLiveMetricsActionTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
            _endpointUtilities = new(_outputHelper);
        }

        [Theory]
        [MemberData(nameof(ActionTestsHelper.GetTfms), MemberType = typeof(ActionTestsHelper))]
        public async Task CollectLiveMetricsAction_Custom(TargetFrameworkMoniker tfm)
        {
            using TemporaryDirectory tempDirectory = new(_outputHelper);

            const string providerName = EventPipe.MonitoringSourceConfiguration.SystemRuntimeEventSourceName;

            var counterNames = new[] { "cpu-usage", "working-set" };

            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                rootOptions.AddGlobalCounter(IntervalSeconds);

                rootOptions.AddFileSystemEgress(ActionTestsConstants.ExpectedEgressProvider, tempDirectory.FullName);

                rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddCollectLiveMetricsAction(ActionTestsConstants.ExpectedEgressProvider, options =>
                    {
                        options.Duration = TimeSpan.FromSeconds(DurationSeconds);
                        options.IncludeDefaultProviders = false;
                        options.Providers = new[]
                        {
                            new EventMetricsProvider
                            {
                                ProviderName = providerName,
                                CounterNames = counterNames,
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

                AppRunner runner = _endpointUtilities.CreateAppRunner(sourceHolder.TransportName, tfm);

                Task<IEndpointInfo> newEndpointInfoTask = callback.WaitAddedEndpointInfoAsync(runner, CommonTestTimeouts.StartProcess);

                await runner.ExecuteAsync(async () =>
                {
                    IEndpointInfo endpointInfo = await newEndpointInfoTask;

                    ICollectionRuleAction action = factory.Create(endpointInfo, options);

                    CollectionRuleActionResult result = await ActionTestsHelper.ExecuteAndDisposeAsync(action, CommonTestTimeouts.LiveMetricsTimeout);

                    string egressPath = ActionTestsHelper.ValidateEgressPath(result);

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
    }
}
