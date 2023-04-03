// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Fixtures;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.HttpApi;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Runners;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    [Collection(DefaultCollectionFixture.Name)]
    public class OverlappingEventSourceTests
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITestOutputHelper _outputHelper;

        public OverlappingEventSourceTests(ITestOutputHelper outputHelper, ServiceProviderFixture serviceProviderFixture)
        {
            _httpClientFactory = serviceProviderFixture.ServiceProvider.GetService<IHttpClientFactory>();
            _outputHelper = outputHelper;
        }

        private const string DefaultRuleName = "FunctionalTestRule";
        private readonly TimeSpan TraceDuration = Timeout.InfiniteTimeSpan;
        private const string AppUrl = "http://+:0";
        private const string AspNetUrlsKey = "ASPNETCORE_Urls";

        /// <summary>
        /// Validates that an AspNetResponseStatus trigger will fire following an HTTP trace.
        /// </summary>
        [Theory(Skip = "https://github.com/dotnet/dotnet-monitor/issues/2762")]
        [InlineData(DiagnosticPortConnectionMode.Listen)]
        public async Task OverlappingEventSourceTests_AspNetResponseStatusTest(DiagnosticPortConnectionMode mode)
        {
            const int ExpectedResponseCount = 2;

            using TemporaryDirectory tempDirectory = new(_outputHelper);
            string ExpectedFilePath = Path.Combine(tempDirectory.FullName, "file.txt");
            string ExpectedFileContent = Guid.NewGuid().ToString("N");
            string[] urlPaths = new string[] { "", "Privacy", "" };

            Task ruleStartedTask = null;
            await ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                mode,
                TestAppScenarios.AspNet.Name,
                appValidate: async (runner, client) =>
                {
                    using ResponseStreamHolder holder = await CaptureTrace(runner, client);

                    await ValidateAspNetTriggerCollected(
                        ruleStartedTask,
                        client,
                        runner.GetLocalhostUrl(),
                        urlPaths,
                        ExpectedFilePath,
                        ExpectedFileContent);

                    await runner.SendCommandAsync(TestAppScenarios.AspNet.Commands.Continue);
                },
                configureTool: (runner) =>
                {
                    runner.ConfigurationFromEnvironment
                        .CreateCollectionRule(DefaultRuleName)
                        .SetAspNetResponseStatusTrigger(options =>
                        {
                            options.ResponseCount = ExpectedResponseCount;
                            options.StatusCodes = new string[] { "200" };
                        })
                        .AddExecuteActionAppAction(Assembly.GetExecutingAssembly(), "TextFileOutput", ExpectedFilePath, ExpectedFileContent);

                    ruleStartedTask = runner.WaitForCollectionRuleActionsCompletedAsync(DefaultRuleName);
                },
                configureApp: (runner) =>
                {
                    runner.Environment.Add(AspNetUrlsKey, AppUrl);
                });
        }

        /// <summary>
        /// Validates that an AspNetRequestDuration trigger will fire following an HTTP trace.
        /// </summary>
        [Theory]
        [InlineData(DiagnosticPortConnectionMode.Listen)]
        public async Task OverlappingEventSourceTests_AspNetRequestDurationTest(DiagnosticPortConnectionMode mode)
        {
            const int ExpectedRequestCount = 2;

            using TemporaryDirectory tempDirectory = new(_outputHelper);
            string ExpectedFilePath = Path.Combine(tempDirectory.FullName, "file.txt");
            string ExpectedFileContent = Guid.NewGuid().ToString("N");
            string[] urlPaths = new string[] { "SlowResponse", "SlowResponse", "SlowResponse" };

            Task ruleStartedTask = null;
            await ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                mode,
                TestAppScenarios.AspNet.Name,
                appValidate: async (runner, client) =>
                {
                    using ResponseStreamHolder holder = await CaptureTrace(runner, client);

                    await ValidateAspNetTriggerCollected(
                        ruleStartedTask,
                        client,
                        runner.GetLocalhostUrl(),
                        urlPaths,
                        ExpectedFilePath,
                        ExpectedFileContent);

                    await runner.SendCommandAsync(TestAppScenarios.AspNet.Commands.Continue);
                },
                configureTool: (runner) =>
                {
                    runner.ConfigurationFromEnvironment
                        .CreateCollectionRule(DefaultRuleName)
                        .SetAspNetRequestDurationTrigger(options =>
                        {
                            options.RequestCount = ExpectedRequestCount;
                            options.RequestDuration = TimeSpan.FromSeconds(0);
                        })
                        .AddExecuteActionAppAction(Assembly.GetExecutingAssembly(), "TextFileOutput", ExpectedFilePath, ExpectedFileContent);

                    ruleStartedTask = runner.WaitForCollectionRuleActionsCompletedAsync(DefaultRuleName);
                },
                configureApp: (runner) =>
                {
                    runner.Environment.Add(AspNetUrlsKey, AppUrl);
                });
        }

        private static async Task ValidateAspNetTriggerCollected(Task ruleTask, ApiClient client, string hostName, string[] paths, string expectedFilePath, string expectedFileContent)
        {
            await ApiCallHelper(hostName, paths, client);

            await ruleTask;
            Assert.True(ruleTask.IsCompleted);

            Assert.True(File.Exists(expectedFilePath));
            Assert.Equal(expectedFileContent, File.ReadAllText(expectedFilePath));

            File.Delete(expectedFilePath);
        }

        private static async Task ApiCallHelper(string hostName, string[] paths, ApiClient client)
        {
            foreach (string path in paths)
            {
                string url = hostName + path;
                _ = await client.ApiCall(url);

                await Task.Delay(TimeSpan.FromMilliseconds(200));
            }
        }

        private async Task<ResponseStreamHolder> CaptureTrace(AppRunner runner, ApiClient client)
        {
            int processId = await runner.ProcessIdTask;

            ResponseStreamHolder holder = await client.CaptureTraceAsync(processId, TraceDuration, WebApi.Models.TraceProfile.Http);
            Assert.NotNull(holder);

            return holder;
        }
    }
}
