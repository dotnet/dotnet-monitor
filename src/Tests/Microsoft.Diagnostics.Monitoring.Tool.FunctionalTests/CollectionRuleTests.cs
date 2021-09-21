// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Fixtures;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Runners;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests
{
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
                        .SetEventCounterTrigger(out EventCounterOptions eventCounterOptions)
                        .AddExecuteActionAppAction("TextFileOutput", ExpectedFilePath, ExpectedFileContent)
                        .SetActionLimits(count: 1);

                    // cpu usage greater that 5% for 2 seconds
                    eventCounterOptions.ProviderName = "System.Runtime";
                    eventCounterOptions.CounterName = "cpu-usage";
                    eventCounterOptions.GreaterThan = 5;
                    eventCounterOptions.SlidingWindowDuration = TimeSpan.FromSeconds(2);

                    ruleCompletedTask = runner.WaitForCollectionRuleCompleteAsync(DefaultRuleName);
                });
        }

        /// <summary>
        /// Validates that a collection rule with a command line filter can be matched to the
        /// target process.
        /// </summary>
        [ConditionalTheory(nameof(IsNotNet5OnUnix))]
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
#endif

        // The GetProcessInfo command is not providing command line arguments (only the process name)
        // for .NET 5 process on non-Windows when suspended. See https://github.com/dotnet/dotnet-monitor/issues/885
        private static bool IsNotNet5OnUnix =>
            DotNetHost.RuntimeVersion.Major != 5 ||
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    }
}
