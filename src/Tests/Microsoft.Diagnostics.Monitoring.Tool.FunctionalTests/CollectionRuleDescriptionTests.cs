// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
using System.Reflection;
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

        private const string NonStartupRuleName = "NonStartupTestRule";
        private const string StartupRuleName = "StartupTestRule";

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

                    // Validate detailed description for the Startup rule

                    CollectionRuleDetailedDescription actualDetailedDescription = await client.GetCollectionRuleDetailedDescriptionAsync(StartupRuleName, await runner.ProcessIdTask, null, null);
                    CollectionRuleDetailedDescription expectedDetailedDescription = new()
                    {
                        ActionCountLimit = CollectionRuleLimitsOptionsDefaults.ActionCount,
                        LifetimeOccurrences = 1,
                        SlidingWindowOccurrences = 1,
                        State = CollectionRuleState.Finished,
                        StateReason = FinishedStartup
                    };
                    Assert.Equal(expectedDetailedDescription, actualDetailedDescription);

                    // Validate brief descriptions for all rules

                    Dictionary<string, CollectionRuleDescription> actualDescriptions = await client.GetCollectionRulesDescriptionAsync(await runner.ProcessIdTask, null, null);
                    Dictionary<string, CollectionRuleDescription> expectedDescriptions = new()
                    {
                        {
                            StartupRuleName, new CollectionRuleDescription()
                            {
                                State = expectedDetailedDescription.State,
                                StateReason = expectedDetailedDescription.StateReason
                            }
                        }
                    };

                    ValidateCollectionRuleDescriptions(expectedDescriptions, actualDescriptions);

                    await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                },
                configureTool: runner =>
                {
                    runner.ConfigurationFromEnvironment.CreateCollectionRule(StartupRuleName)
                        .SetStartupTrigger();

                    ruleCompletedTask = runner.WaitForCollectionRuleCompleteAsync(StartupRuleName);
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
            await RetryUtilities.RetryAsync(
                func: async () =>
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
                            // Validate detailed description for the NonStartup rule before spinning the CPU

                            CollectionRuleDetailedDescription actualDetailedDescription_Before = await client.GetCollectionRuleDetailedDescriptionAsync(NonStartupRuleName, await runner.ProcessIdTask, null, null);
                            CollectionRuleDetailedDescription expectedDetailedDescription_Before = new()
                            {
                                ActionCountLimit = ExpectedActionCountLimit,
                                LifetimeOccurrences = 0,
                                SlidingWindowOccurrences = 0,
                                State = CollectionRuleState.Running,
                                StateReason = Running
                            };
                            Assert.Equal(expectedDetailedDescription_Before, actualDetailedDescription_Before);

                            // Validate brief descriptions for all rules before spinning the CPU

                            Dictionary<string, CollectionRuleDescription> actualDescriptions_Before = await client.GetCollectionRulesDescriptionAsync(await runner.ProcessIdTask, null, null);
                            Dictionary<string, CollectionRuleDescription> expectedDescriptions_Before = new()
                            {
                                    {
                                        NonStartupRuleName, new CollectionRuleDescription()
                                        {
                                            State = expectedDetailedDescription_Before.State,
                                            StateReason = expectedDetailedDescription_Before.StateReason
                                        }
                                    }
                            };

                            ValidateCollectionRuleDescriptions(expectedDescriptions_Before, actualDescriptions_Before);

                            await runner.SendCommandAsync(TestAppScenarios.SpinWait.Commands.StartSpin);

                            await ruleCompletedTask;

                            await runner.SendCommandAsync(TestAppScenarios.SpinWait.Commands.StopSpin);

                            // Validate detailed description for the NonStartup rule after spinning the CPU

                            CollectionRuleDetailedDescription actualDetailedDescription_After = await client.GetCollectionRuleDetailedDescriptionAsync(NonStartupRuleName, await runner.ProcessIdTask, null, null);
                            CollectionRuleDetailedDescription expectedDetailedDescription_After = new()
                            {
                                ActionCountLimit = ExpectedActionCountLimit,
                                LifetimeOccurrences = 1,
                                SlidingWindowOccurrences = 1,
                                State = CollectionRuleState.Finished,
                                StateReason = FinishedActionCount
                            };
                            Assert.Equal(expectedDetailedDescription_After, actualDetailedDescription_After);

                            // Validate brief descriptions for all rules after spinning the CPU

                            Dictionary<string, CollectionRuleDescription> actualDescriptions_After = await client.GetCollectionRulesDescriptionAsync(await runner.ProcessIdTask, null, null);
                            Dictionary<string, CollectionRuleDescription> expectedDescriptions_After = new()
                            {
                                    {
                                        NonStartupRuleName, new CollectionRuleDescription()
                                        {
                                            State = expectedDetailedDescription_After.State,
                                            StateReason = expectedDetailedDescription_After.StateReason
                                        }
                                    }
                            };

                            ValidateCollectionRuleDescriptions(expectedDescriptions_After, actualDescriptions_After);
                        },
                        configureTool: runner =>
                        {
                            runner.ConfigurationFromEnvironment.CreateCollectionRule(NonStartupRuleName)
                                .SetEventCounterTrigger(options =>
                                {
                                    // cpu usage greater than 1% for 1 second
                                    options.ProviderName = "System.Runtime";
                                    options.CounterName = "cpu-usage";
                                    options.GreaterThan = 1;
                                    options.SlidingWindowDuration = TimeSpan.FromSeconds(1);
                                })
                                .AddExecuteActionAppAction(Assembly.GetExecutingAssembly(), "TextFileOutput", ExpectedFilePath, ExpectedFileContent)
                                .SetActionLimits(count: ExpectedActionCountLimit);

                            ruleCompletedTask = runner.WaitForCollectionRuleCompleteAsync(NonStartupRuleName);
                        });
                },
                shouldRetry: (Exception ex) => ex is TaskCanceledException,
                outputHelper: _outputHelper);
        }

        /// <summary>
        /// Validates the CollectionRuleDescriptions for two rules running on the same process
        /// </summary>
        [Theory]
        [InlineData(DiagnosticPortConnectionMode.Listen)]
        public async Task CollectionRuleDescription_MultipleRulesTest(DiagnosticPortConnectionMode mode)
        {
            await RetryUtilities.RetryAsync(
                func: async () =>
                {
                    using TemporaryDirectory tempDirectory = new(_outputHelper);
                        string ExpectedFilePath = Path.Combine(tempDirectory.FullName, "file.txt");
                        string ExpectedFileContent = Guid.NewGuid().ToString("N");

                        const int ExpectedActionCountLimit = 1;

                        Task ruleCompletedTask_Startup = null;
                        Task ruleCompletedTask_NonStartup = null;

                        await ScenarioRunner.SingleTarget(
                            _outputHelper,
                            _httpClientFactory,
                            mode,
                            TestAppScenarios.SpinWait.Name,
                            appValidate: async (runner, client) =>
                            {
                                await ruleCompletedTask_Startup;

                                // Validate detailed description for the NonStartup rule
                                CollectionRuleDetailedDescription actualDetailedDescription_NonStartup = await client.GetCollectionRuleDetailedDescriptionAsync(NonStartupRuleName, await runner.ProcessIdTask, null, null);
                                CollectionRuleDetailedDescription expectedDetailedDescription_NonStartup = new()
                                {
                                    ActionCountLimit = ExpectedActionCountLimit,
                                    LifetimeOccurrences = 0,
                                    SlidingWindowOccurrences = 0,
                                    State = CollectionRuleState.Running,
                                    StateReason = Running
                                };
                                Assert.Equal(expectedDetailedDescription_NonStartup, actualDetailedDescription_NonStartup);

                                // Validate detailed description for the Startup rule

                                CollectionRuleDetailedDescription actualDetailedDescription_Startup = await client.GetCollectionRuleDetailedDescriptionAsync(StartupRuleName, await runner.ProcessIdTask, null, null);
                                CollectionRuleDetailedDescription expectedDetailedDescription_Startup = new()
                                {
                                    ActionCountLimit = CollectionRuleLimitsOptionsDefaults.ActionCount,
                                    LifetimeOccurrences = 1,
                                    SlidingWindowOccurrences = 1,
                                    State = CollectionRuleState.Finished,
                                    StateReason = FinishedStartup
                                };
                                Assert.Equal(expectedDetailedDescription_Startup, actualDetailedDescription_Startup);

                                // Validate brief descriptions for all rules

                                Dictionary<string, CollectionRuleDescription> actualDescriptions = await client.GetCollectionRulesDescriptionAsync(await runner.ProcessIdTask, null, null);
                                Dictionary<string, CollectionRuleDescription> expectedDescriptions = new()
                                {
                                    {
                                        NonStartupRuleName, new CollectionRuleDescription()
                                        {
                                            State = expectedDetailedDescription_NonStartup.State,
                                            StateReason = expectedDetailedDescription_NonStartup.StateReason
                                        }
                                    },
                                    {
                                        StartupRuleName, new CollectionRuleDescription()
                                        {
                                            State = expectedDetailedDescription_Startup.State,
                                            StateReason = expectedDetailedDescription_Startup.StateReason
                                        }
                                    }
                                };

                                ValidateCollectionRuleDescriptions(expectedDescriptions, actualDescriptions);

                                await runner.SendCommandAsync(TestAppScenarios.SpinWait.Commands.StartSpin);

                                await ruleCompletedTask_NonStartup;

                                await runner.SendCommandAsync(TestAppScenarios.SpinWait.Commands.StopSpin);

                                // Validate detailed description for the NonStartup rule after spinning the CPU

                                CollectionRuleDetailedDescription actualDetailedDescription_After = await client.GetCollectionRuleDetailedDescriptionAsync(NonStartupRuleName, await runner.ProcessIdTask, null, null);
                                CollectionRuleDetailedDescription expectedDetailedDescription_After = new()
                                {
                                    ActionCountLimit = ExpectedActionCountLimit,
                                    LifetimeOccurrences = 1,
                                    SlidingWindowOccurrences = 1,
                                    State = CollectionRuleState.Finished,
                                    StateReason = FinishedActionCount
                                };
                                Assert.Equal(expectedDetailedDescription_After, actualDetailedDescription_After);

                                // Validate brief descriptions for all rules after spinning the CPU

                                Dictionary<string, CollectionRuleDescription> actualDescriptions_After = await client.GetCollectionRulesDescriptionAsync(await runner.ProcessIdTask, null, null);
                                Dictionary<string, CollectionRuleDescription> expectedDescriptions_After = new()
                                {
                                    {
                                        NonStartupRuleName, new CollectionRuleDescription()
                                        {
                                            State = expectedDetailedDescription_After.State,
                                            StateReason = expectedDetailedDescription_After.StateReason
                                        }
                                    },
                                    {
                                        StartupRuleName, new CollectionRuleDescription()
                                        {
                                            State = expectedDetailedDescription_Startup.State,
                                            StateReason = expectedDetailedDescription_Startup.StateReason
                                        }
                                    }
                                };

                                ValidateCollectionRuleDescriptions(expectedDescriptions_After, actualDescriptions_After);

                            },
                            configureTool: runner =>
                            {
                                runner.ConfigurationFromEnvironment.CreateCollectionRule(NonStartupRuleName)
                                    .SetEventCounterTrigger(options =>
                                    {
                                        // cpu usage greater than 1% for 1 second
                                        options.ProviderName = "System.Runtime";
                                        options.CounterName = "cpu-usage";
                                        options.GreaterThan = 1;
                                        options.SlidingWindowDuration = TimeSpan.FromSeconds(1);
                                    })
                                    .AddExecuteActionAppAction(Assembly.GetExecutingAssembly(), "TextFileOutput", ExpectedFilePath, ExpectedFileContent)
                                    .SetActionLimits(count: ExpectedActionCountLimit);

                                runner.ConfigurationFromEnvironment.CreateCollectionRule(StartupRuleName)
                                    .SetStartupTrigger();

                                ruleCompletedTask_Startup = runner.WaitForCollectionRuleCompleteAsync(StartupRuleName);
                                ruleCompletedTask_NonStartup = runner.WaitForCollectionRuleCompleteAsync(NonStartupRuleName);
                            });
                },
                shouldRetry: (Exception ex) => ex is TaskCanceledException,
                outputHelper: _outputHelper);
        }

        private static void ValidateCollectionRuleDescriptions(Dictionary<string, CollectionRuleDescription> expectedCollectionRuleDescriptions, Dictionary<string, CollectionRuleDescription> actualCollectionRuleDescriptions)
        {
            Assert.Equal(actualCollectionRuleDescriptions.Keys.Count, expectedCollectionRuleDescriptions.Keys.Count);

            foreach (var key in actualCollectionRuleDescriptions.Keys)
            {
                CollectionRuleDescription actualDescription = actualCollectionRuleDescriptions[key];
                CollectionRuleDescription expectedDescription = expectedCollectionRuleDescriptions[key];

                Assert.Equal(expectedDescription, actualDescription);
            }
        }
    }
}
