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
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
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

        // These should be identical to the messages found in Strings.resx
        private const string FinishedStartup = "The collection rule will no longer trigger because the Startup trigger only executes once.";
        private const string FinishedActionCount = "The collection rule will no longer trigger because the ActionCount was reached.";
        private const string Running = "This collection rule is active and waiting for its triggering conditions to be satisfied.";

        /// <summary>
        /// Validates that a startup rule will execute and complete with the correct collection rule descriptions
        /// </summary>
        [Theory]
        [InlineData(DiagnosticPortConnectionMode.Listen)]
        public async Task CollectionRuleDescription_StartupTriggerTest(DiagnosticPortConnectionMode mode)
        {
            using TemporaryDirectory tempDirectory = new(_outputHelper);

            Task ruleCompletedTask = null;

            await ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                mode,
                TestAppScenarios.AsyncWait.Name,
                appValidate: async (runner, client) =>
                {
                    await ruleCompletedTask;

                    Dictionary<string, CollectionRuleDescription> collectionRuleDescriptions = await client.GetCollectionRulesDescriptionAsync(await runner.ProcessIdTask, null, null);

                    Dictionary<string, CollectionRuleDescription> expectedDescriptions = new();

                    expectedDescriptions.Add(DefaultRuleName, new() {
                        ActionCountLimit = CollectionRuleLimitsOptionsDefaults.ActionCount,
                        LifetimeOccurrences = 1,
                        SlidingWindowOccurrences = 1,
                        State = CollectionRuleState.Finished,
                        StateReason = FinishedStartup
                    });

                    ValidateCollectionRuleDescriptions(collectionRuleDescriptions, expectedDescriptions);

                    await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                },
                configureTool: runner =>
                {
                    runner.ConfigurationFromEnvironment.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger();

                    ruleCompletedTask = runner.WaitForCollectionRuleCompleteAsync(DefaultRuleName);
                });
        }

        /// <summary>
        /// Validates that a non-startup rule will complete when it has an action limit specified
        /// without a sliding window duration.
        /// </summary>
        [Theory]
        [InlineData(DiagnosticPortConnectionMode.Listen)]
        public async Task CollectionRuleDescription_ActionLimitTest(DiagnosticPortConnectionMode mode)
        {
            using TemporaryDirectory tempDirectory = new(_outputHelper);
            string ExpectedFilePath = Path.Combine(tempDirectory.FullName, "file.txt");
            string ExpectedFileContent = Guid.NewGuid().ToString("N");

            const int ExpectedActionCountLimit = 1;

            Task ruleCompletedTask = null;

            await ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                mode,
                TestAppScenarios.SpinWait.Name,
                appValidate: async (runner, client) =>
                {
                    Dictionary<string, CollectionRuleDescription> collectionRuleDescriptionsBefore = await client.GetCollectionRulesDescriptionAsync(await runner.ProcessIdTask, null, null);

                    Dictionary<string, CollectionRuleDescription> expectedDescriptionsBefore = new();

                    expectedDescriptionsBefore.Add(DefaultRuleName, new() {
                        ActionCountLimit = ExpectedActionCountLimit,
                        LifetimeOccurrences = 0,
                        SlidingWindowOccurrences = 0,
                        State = CollectionRuleState.Running,
                        StateReason = Running
                    });

                    ValidateCollectionRuleDescriptions(collectionRuleDescriptionsBefore, expectedDescriptionsBefore);

                    await runner.SendCommandAsync(TestAppScenarios.SpinWait.Commands.StartSpin);

                    await ruleCompletedTask;

                    await runner.SendCommandAsync(TestAppScenarios.SpinWait.Commands.StopSpin);

                    Dictionary<string, CollectionRuleDescription> collectionRuleDescriptionsAfter = await client.GetCollectionRulesDescriptionAsync(await runner.ProcessIdTask, null, null);

                    Dictionary<string, CollectionRuleDescription> expectedDescriptionsAfter = new();

                    expectedDescriptionsAfter.Add(DefaultRuleName, new() {
                        ActionCountLimit = ExpectedActionCountLimit,
                        LifetimeOccurrences = 1,
                        SlidingWindowOccurrences = 1,
                        State = CollectionRuleState.Finished,
                        StateReason = FinishedActionCount
                    });

                    ValidateCollectionRuleDescriptions(collectionRuleDescriptionsAfter, expectedDescriptionsAfter);
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
                        .SetActionLimits(count: ExpectedActionCountLimit);

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

                Assert.Equal(expectedDescription, actualDescription);
            }
        }
#endif
    }
}
