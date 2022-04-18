// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Fixtures;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.HttpApi;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Runners;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    [Collection(DefaultCollectionFixture.Name)]
    public class CollectionRuleDescriptionTests
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITestOutputHelper _outputHelper;

        public CollectionRuleDescriptionTests(ITestOutputHelper outputHelper, ServiceProviderFixture serviceProviderFixture)
        {
            _httpClientFactory = serviceProviderFixture.ServiceProvider.GetService<IHttpClientFactory>();
            _outputHelper = outputHelper;
        }

#if NET5_0_OR_GREATER
        private const string DefaultRuleName = "FunctionalTestRule";

        /// <summary>
        /// Validates that a startup rule will execute and complete without action beyond
        /// discovering the target process.
        /// </summary>
        [Theory]
        [InlineData(DiagnosticPortConnectionMode.Listen)]
        public async Task CollectionRule_StartupTriggerTest(DiagnosticPortConnectionMode mode)
        {
            using TemporaryDirectory tempDirectory = new(_outputHelper);
            string ExpectedFilePath = Path.Combine(tempDirectory.FullName, "file.txt");
            string ExpectedFileContent = Guid.NewGuid().ToString("N");

            Task ruleCompletedTask = null;

            await ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                mode,
                TestAppScenarios.AsyncWait.Name,
                appValidate: async (runner, client) =>
                {
                    await ruleCompletedTask;

                    Assert.True(File.Exists(ExpectedFilePath));
                    Assert.Equal(ExpectedFileContent, File.ReadAllText(ExpectedFilePath));

                    Dictionary<string, CollectionRuleDescription> collectionRuleDescriptions = await client.GetCollectionRulesDescriptionAsync(await runner.ProcessIdTask, null, null);

                    Dictionary<string, CollectionRuleDescription> expectedDescriptions = new();

                    expectedDescriptions.Add(DefaultRuleName, new()
                    {
                        LifetimeOccurrences = 1,
                        SlidingWindowOccurrences = 1,
                        State = CollectionRulesState.Finished,
                        StateReason = CollectionRulesStateReasons.Finished_Startup
                    });

                    ValidateCollectionRuleDescriptions(collectionRuleDescriptions, expectedDescriptions);

                    await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                },
                configureTool: runner =>
                {
                    runner.ConfigurationFromEnvironment.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddExecuteActionAppAction("TextFileOutput", ExpectedFilePath, ExpectedFileContent);

                    ruleCompletedTask = runner.WaitForCollectionRuleCompleteAsync(DefaultRuleName);
                });
        }

        private void ValidateCollectionRuleDescriptions(Dictionary<string, CollectionRuleDescription> actualCollectionRuleDescriptions, Dictionary<string, CollectionRuleDescription> expectedCollectionRuleDescriptions)
        {
            Assert.Equal(actualCollectionRuleDescriptions.Keys.Count, expectedCollectionRuleDescriptions.Keys.Count);

            foreach (var key in actualCollectionRuleDescriptions.Keys)
            {
                CollectionRuleDescription actualDescription = actualCollectionRuleDescriptions[key];
                CollectionRuleDescription expectedDescription = expectedCollectionRuleDescriptions[key];

                Assert.Equal(actualDescription.ActionCountLimit, expectedDescription.ActionCountLimit);
                Assert.Equal(actualDescription.ActionCountSlidingWindowDurationLimit, expectedDescription.ActionCountSlidingWindowDurationLimit);
                Assert.Equal(actualDescription.LifetimeOccurrences, expectedDescription.LifetimeOccurrences);
                Assert.Equal(actualDescription.RuleFinishedCountdown, expectedDescription.RuleFinishedCountdown);
                Assert.Equal(actualDescription.SlidingWindowDurationCountdown, expectedDescription.SlidingWindowDurationCountdown);
                Assert.Equal(actualDescription.SlidingWindowOccurrences, expectedDescription.SlidingWindowOccurrences);
                Assert.Equal(actualDescription.State, expectedDescription.State);
                Assert.Equal(actualDescription.StateReason, expectedDescription.StateReason);
            }
        }
        
        /// <summary>
        /// Validates that a non-startup rule will complete when it has an action limit specified
        /// without a sliding window duration.
        /// </summary>
        [Theory]
        [InlineData(DiagnosticPortConnectionMode.Listen)]
        public async Task CollectionRule_ActionLimitTest(DiagnosticPortConnectionMode mode)
        {
            using TemporaryDirectory tempDirectory = new(_outputHelper);
            string ExpectedFilePath = Path.Combine(tempDirectory.FullName, "file.txt");
            string ExpectedFileContent = Guid.NewGuid().ToString("N");

            Task ruleCompletedTask = null;

            await ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                mode,
                TestAppScenarios.SpinWait.Name,
                appValidate: async (runner, client) =>
                {
                    Dictionary<string, CollectionRuleDescription> collectionRuleDescriptions_Before = await client.GetCollectionRulesDescriptionAsync(await runner.ProcessIdTask, null, null);

                    Dictionary<string, CollectionRuleDescription> expectedDescriptions_Before = new();

                    expectedDescriptions_Before.Add(DefaultRuleName, new()
                    {
                        ActionCountLimit = 1,
                        LifetimeOccurrences = 0,
                        SlidingWindowOccurrences = 0,
                        State = CollectionRulesState.Running,
                        StateReason = CollectionRulesStateReasons.Running
                    });

                    ValidateCollectionRuleDescriptions(collectionRuleDescriptions_Before, expectedDescriptions_Before);

                    await runner.SendCommandAsync(TestAppScenarios.SpinWait.Commands.StartSpin);

                    await ruleCompletedTask;

                    await runner.SendCommandAsync(TestAppScenarios.SpinWait.Commands.StopSpin);

                    Assert.True(File.Exists(ExpectedFilePath));
                    Assert.Equal(ExpectedFileContent, File.ReadAllText(ExpectedFilePath));

                    Dictionary<string, CollectionRuleDescription> collectionRuleDescriptions_After = await client.GetCollectionRulesDescriptionAsync(await runner.ProcessIdTask, null, null);

                    Dictionary<string, CollectionRuleDescription> expectedDescriptions_After = new();

                    expectedDescriptions_After.Add(DefaultRuleName, new()
                    {
                        ActionCountLimit = 1,
                        LifetimeOccurrences = 1,
                        SlidingWindowOccurrences = 1,
                        State = CollectionRulesState.Finished,
                        StateReason = CollectionRulesStateReasons.Finished_ActionCount
                    });

                    ValidateCollectionRuleDescriptions(collectionRuleDescriptions_After, expectedDescriptions_After);
                },
                configureTool: runner =>
                {
                    runner.ConfigurationFromEnvironment.CreateCollectionRule(DefaultRuleName)
                        .SetEventCounterTrigger(options =>
                        {
                            // cpu usage greater that 5% for 2 seconds
                            options.ProviderName = "System.Runtime";
                            options.CounterName = "cpu-usage";
                            options.GreaterThan = 5;
                            options.SlidingWindowDuration = TimeSpan.FromSeconds(2);
                        })
                        .AddExecuteActionAppAction("TextFileOutput", ExpectedFilePath, ExpectedFileContent)
                        .SetActionLimits(count: 1);

                    ruleCompletedTask = runner.WaitForCollectionRuleCompleteAsync(DefaultRuleName);
                });
        }
#endif
    }
}
