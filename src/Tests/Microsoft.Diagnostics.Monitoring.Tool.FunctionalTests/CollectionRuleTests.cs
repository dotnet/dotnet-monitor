﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Fixtures;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Runners;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Extensions.DependencyInjection;
using System;
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
    public class CollectionRuleTests
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITestOutputHelper _outputHelper;

        public CollectionRuleTests(ITestOutputHelper outputHelper, ServiceProviderFixture serviceProviderFixture)
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
                    await runner.SendCommandAsync(TestAppScenarios.SpinWait.Commands.StartSpin);

                    await ruleCompletedTask;

                    await runner.SendCommandAsync(TestAppScenarios.SpinWait.Commands.StopSpin);

                    Assert.True(File.Exists(ExpectedFilePath));
                    Assert.Equal(ExpectedFileContent, File.ReadAllText(ExpectedFilePath));
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

        /// <summary>
        /// Validates that a collection rule with a command line filter can be matched to the
        /// target process.
        /// </summary>
        [ConditionalTheory(nameof(IsNotNet5OrGreaterOnUnix))]
        [InlineData(DiagnosticPortConnectionMode.Listen)]
        public async Task CollectionRule_CommandLineFilterMatchTest(DiagnosticPortConnectionMode mode)
        {
            Task startedTask = null;

            await ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                mode,
                TestAppScenarios.AsyncWait.Name,
                appValidate: async (runner, client) =>
                {
                    await startedTask;

                    await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                },
                configureTool: runner =>
                {
                    runner.ConfigurationFromEnvironment.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddCommandLineFilter(TestAppScenarios.AsyncWait.Name);

                    startedTask = runner.WaitForCollectionRuleStartedAsync(DefaultRuleName);
                });
        }

        /// <summary>
        /// Validates that a collection rule with a command line filter can fail to match the
        /// target process.
        /// </summary>
        [Theory]
        [InlineData(DiagnosticPortConnectionMode.Listen)]
        public async Task CollectionRule_CommandLineFilterNoMatchTest(DiagnosticPortConnectionMode mode)
        {
            Task filteredTask = null;

            await ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                mode,
                TestAppScenarios.AsyncWait.Name,
                appValidate: async (runner, client) =>
                {
                    await filteredTask;

                    await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                },
                configureTool: runner =>
                {
                    // Note that the process name filter is specified as "SpinWait" whereas the
                    // actual command line of the target process will contain "AsyncWait".
                    runner.ConfigurationFromEnvironment.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddProcessNameFilter(TestAppScenarios.SpinWait.Name);

                    filteredTask = runner.WaitForCollectionRuleUnmatchedFiltersAsync(DefaultRuleName);
                });
        }

        /// <summary>
        /// Validates that a collection rule with a process name filter can be matched to the
        /// target process.
        /// </summary>
        [Theory]
        [InlineData(DiagnosticPortConnectionMode.Listen)]
        public async Task CollectionRule_ProcessNameFilterMatchTest(DiagnosticPortConnectionMode mode)
        {
            Task startedTask = null;

            await ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                mode,
                TestAppScenarios.AsyncWait.Name,
                appValidate: async (runner, client) =>
                {
                    await startedTask;

                    await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                },
                configureTool: runner =>
                {
                    runner.ConfigurationFromEnvironment.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddProcessNameFilter(DotNetHost.HostExeNameWithoutExtension);

                    startedTask = runner.WaitForCollectionRuleStartedAsync(DefaultRuleName);
                });
        }

        /// <summary>
        /// Validates that a collection rule with a process name filter can fail to match the
        /// target process.
        /// </summary>
        [Theory]
        [InlineData(DiagnosticPortConnectionMode.Listen)]
        public async Task CollectionRule_ProcessNameFilterNoMatchTest(DiagnosticPortConnectionMode mode)
        {
            Task filteredTask = null;

            await ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                mode,
                TestAppScenarios.AsyncWait.Name,
                appValidate: async (runner, client) =>
                {
                    await filteredTask;

                    await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                },
                configureTool: runner =>
                {
                    runner.ConfigurationFromEnvironment.CreateCollectionRule(DefaultRuleName)
                        .SetStartupTrigger()
                        .AddProcessNameFilter("UmatchedName");

                    filteredTask = runner.WaitForCollectionRuleUnmatchedFiltersAsync(DefaultRuleName);
                });
        }

        /// <summary>
        /// Validates that a change in the collection rule configuration is detected and applied correctly.
        /// </summary>
        [Theory]
        [InlineData(DiagnosticPortConnectionMode.Listen)]
        public async Task CollectionRule_ConfigurationChangeTest(DiagnosticPortConnectionMode mode)
        {
            const string firstRuleName = "FirstRule";
            const string secondRuleName = "SecondRule";

            DiagnosticPortHelper.Generate(
                mode,
                out DiagnosticPortConnectionMode appConnectionMode,
                out string diagnosticPortPath);

            await using MonitorCollectRunner toolRunner = new(_outputHelper);
            toolRunner.ConnectionMode = mode;
            toolRunner.DiagnosticPortPath = diagnosticPortPath;
            toolRunner.DisableAuthentication = true;

            // Create a rule with some settings
            RootOptions originalOptions = new();
            originalOptions.CreateCollectionRule(firstRuleName)
                .SetStartupTrigger();

            await toolRunner.WriteUserSettingsAsync(originalOptions);

            await toolRunner.StartAsync();

            AppRunner appRunner = new(_outputHelper, Assembly.GetExecutingAssembly());
            appRunner.ConnectionMode = appConnectionMode;
            appRunner.DiagnosticPortPath = diagnosticPortPath;
            appRunner.ScenarioName = TestAppScenarios.AsyncWait.Name;

            Task originalActionsCompletedTask = toolRunner.WaitForCollectionRuleActionsCompletedAsync(firstRuleName);

            await appRunner.ExecuteAsync(async () =>
            {
                // Validate that the first rule is observed and its actions are run.
                await originalActionsCompletedTask;

                // Set up new observers for the first and second rule.
                originalActionsCompletedTask = toolRunner.WaitForCollectionRuleActionsCompletedAsync(firstRuleName);
                Task newActionsCompletedTask = toolRunner.WaitForCollectionRuleActionsCompletedAsync(secondRuleName);

                // Change collection rule configuration to only contain the second rule.
                RootOptions newOptions = new();
                newOptions.CreateCollectionRule(secondRuleName)
                    .SetStartupTrigger();

                await toolRunner.WriteUserSettingsAsync(newOptions);

                // Validate that only the second rule is observed.
                await newActionsCompletedTask;
                Assert.False(originalActionsCompletedTask.IsCompleted);

                await appRunner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
            });
            Assert.Equal(0, appRunner.ExitCode);
        }

        /// <summary>
        /// Validates that when a process exits, the collection rules for the process are stopped.
        /// </summary>
        [Theory]
        [InlineData(DiagnosticPortConnectionMode.Listen)]
        public async Task CollectionRule_StoppedOnExitTest(DiagnosticPortConnectionMode mode)
        {
            DiagnosticPortHelper.Generate(
                mode,
                out DiagnosticPortConnectionMode appConnectionMode,
                out string diagnosticPortPath);

            await using MonitorCollectRunner toolRunner = new(_outputHelper);
            toolRunner.ConnectionMode = mode;
            toolRunner.DiagnosticPortPath = diagnosticPortPath;
            toolRunner.DisableAuthentication = true;

            // Create a rule with some settings
            RootOptions originalOptions = new();
            originalOptions.CreateCollectionRule(DefaultRuleName)
                .SetEventCounterTrigger(options =>
                {
                    options.ProviderName = "System.Runtime";
                    options.CounterName = "cpu-usage";
                    options.GreaterThan = 1000; // Intentionally unobtainable
                    options.SlidingWindowDuration = TimeSpan.FromSeconds(1);
                });

            await toolRunner.WriteUserSettingsAsync(originalOptions);

            await toolRunner.StartAsync();

            AppRunner appRunner = new(_outputHelper, Assembly.GetExecutingAssembly());
            appRunner.ConnectionMode = appConnectionMode;
            appRunner.DiagnosticPortPath = diagnosticPortPath;
            appRunner.ScenarioName = TestAppScenarios.AsyncWait.Name;

            Task ruleStartedTask = toolRunner.WaitForCollectionRuleStartedAsync(DefaultRuleName);
            Task rulesStoppedTask = toolRunner.WaitForCollectionRulesStoppedAsync();

            await appRunner.ExecuteAsync(async () =>
            {
                await ruleStartedTask;

                await appRunner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
            });
            Assert.Equal(0, appRunner.ExitCode);

            // All of the rules for the process should have stopped. Note that dotnet-monitor has
            // not yet exited at this point in time; this is verification that the rules have stopped
            // for the target process before dotnet-monitor shuts down.
            await rulesStoppedTask;
        }

        // The GetProcessInfo command is not providing command line arguments (only the process name)
        // for .NET 5+ process on non-Windows when suspended. See https://github.com/dotnet/dotnet-monitor/issues/885
        private static bool IsNotNet5OrGreaterOnUnix =>
            DotNetHost.RuntimeVersion.Major < 5 ||
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

#endif
    }
}
