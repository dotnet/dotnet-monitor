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
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    [Collection(DefaultCollectionFixture.Name)]
    public class CollectionRuleAndApiTests
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITestOutputHelper _outputHelper;

        public CollectionRuleAndApiTests(ITestOutputHelper outputHelper, ServiceProviderFixture serviceProviderFixture)
        {
            _httpClientFactory = serviceProviderFixture.ServiceProvider.GetService<IHttpClientFactory>();
            _outputHelper = outputHelper;
        }

#if NET5_0_OR_GREATER
        private const string DefaultRuleName = "FunctionalTestRule";
        private readonly TimeSpan TraceDuration = TimeSpan.FromSeconds(1);
        private const string hostName = "http://localhost:82";
        private const string additionalArguments = "--urls http://0.0.0.0:82";

        /// <summary>
        /// Validates that an AspNetResponseStatus trigger will fire following an HTTP trace.
        /// </summary>
        //[Theory(Skip = "These tests will fail until #3425 in the diagnostics repo is checked in.")]
        [Theory]
        [InlineData(DiagnosticPortConnectionMode.Listen)]
        public async Task CollectionRuleAndApi_AspNetResponseStatusTest(DiagnosticPortConnectionMode mode)
        {
            const int ExpectedResponseCount = 2;

            using TemporaryDirectory tempDirectory = new(_outputHelper);
            string ExpectedFilePath = Path.Combine(tempDirectory.FullName, "file.txt");
            string ExpectedFileContent = Guid.NewGuid().ToString("N");
            string[] urlPaths = new string[] { "", "/Privacy", "" };

            DiagnosticPortHelper.Generate(
                mode,
                out DiagnosticPortConnectionMode appConnectionMode,
                out string diagnosticPortPath);

            await using MonitorCollectRunner toolRunner = new(_outputHelper);
            toolRunner.ConnectionMode = mode;
            toolRunner.DiagnosticPortPath = diagnosticPortPath;
            toolRunner.DisableAuthentication = true;

            AppRunner appRunner = SetUpAppRunner(appConnectionMode, diagnosticPortPath);

            await appRunner.ExecuteAsync(async () =>
            {
                RootOptions newOptions = new();
                newOptions.CreateCollectionRule(DefaultRuleName)
                        .SetAspNetResponseStatusTrigger(options =>
                        {
                            options.ResponseCount = ExpectedResponseCount;
                            options.StatusCodes = new string[] { "200", "202" };
                        })
                        .AddExecuteActionAppAction("TextFileOutput", ExpectedFilePath, ExpectedFileContent);

                await toolRunner.WriteUserSettingsAsync(newOptions);
                await toolRunner.StartAsync();

                using HttpClient httpClient = await toolRunner.CreateHttpClientDefaultAddressAsync(_httpClientFactory);
                ApiClient apiClient = new(_outputHelper, httpClient);

                await CaptureTrace(appRunner, apiClient);

                Task ruleStartedTask = toolRunner.WaitForCollectionRuleActionsCompletedAsync(DefaultRuleName);

                await ValidateAspNetTriggerCollected(
                    ruleStartedTask,
                    apiClient,
                    hostName,
                    urlPaths,
                    ExpectedFilePath,
                    ExpectedFileContent);

                appRunner.KillProcess();
            }, noScenario: true);
        }

        /// <summary>
        /// Validates that an AspNetRequestDuration trigger will fire following an HTTP trace.
        /// </summary>
        //[Theory(Skip = "These tests will fail until #3425 in the diagnostics repo is checked in.")]
        [Theory]
        [InlineData(DiagnosticPortConnectionMode.Listen)]
        public async Task CollectionRuleAndApi_AspNetRequestDurationTest(DiagnosticPortConnectionMode mode)
        {
            const int ExpectedRequestCount = 2;

            using TemporaryDirectory tempDirectory = new(_outputHelper);
            string ExpectedFilePath = Path.Combine(tempDirectory.FullName, "file.txt");
            string ExpectedFileContent = Guid.NewGuid().ToString("N");
            string[] urlPaths = new string[] { "/SlowResponse", "/SlowResponse", "/SlowResponse" };

            DiagnosticPortHelper.Generate(
                mode,
                out DiagnosticPortConnectionMode appConnectionMode,
                out string diagnosticPortPath);

            await using MonitorCollectRunner toolRunner = new(_outputHelper);
            toolRunner.ConnectionMode = mode;
            toolRunner.DiagnosticPortPath = diagnosticPortPath;
            toolRunner.DisableAuthentication = true;

            AppRunner appRunner = SetUpAppRunner(appConnectionMode, diagnosticPortPath);

            await appRunner.ExecuteAsync(async () =>
            {
                RootOptions newOptions = new();
                newOptions.CreateCollectionRule(DefaultRuleName)
                        .SetAspNetRequestDurationTrigger(options =>
                        {
                            options.RequestCount = ExpectedRequestCount;
                            options.RequestDuration = TimeSpan.FromSeconds(0);
                        })
                        .AddExecuteActionAppAction("TextFileOutput", ExpectedFilePath, ExpectedFileContent);

                await toolRunner.WriteUserSettingsAsync(newOptions);
                await toolRunner.StartAsync();

                using HttpClient httpClient = await toolRunner.CreateHttpClientDefaultAddressAsync(_httpClientFactory);
                ApiClient apiClient = new(_outputHelper, httpClient);

                await CaptureTrace(appRunner, apiClient);

                Task ruleStartedTask = toolRunner.WaitForCollectionRuleActionsCompletedAsync(DefaultRuleName);

                await ValidateAspNetTriggerCollected(
                    ruleStartedTask,
                    apiClient,
                    hostName,
                    urlPaths,
                    ExpectedFilePath,
                    ExpectedFileContent);

                appRunner.KillProcess();
            }, noScenario: true);
        }

        private async Task ValidateAspNetTriggerCollected(Task ruleTask, ApiClient client, string hostName, string[] paths, string expectedFilePath, string expectedFileContent)
        {
            await ApiCallHelper(hostName, paths, client);

            await ruleTask;
            Assert.True(ruleTask.IsCompleted);

            Assert.True(File.Exists(expectedFilePath));
            Assert.Equal(expectedFileContent, File.ReadAllText(expectedFilePath));

            File.Delete(expectedFilePath);
        }

        private AppRunner SetUpAppRunner(DiagnosticPortConnectionMode appConnectionMode, string diagnosticPortPath)
        {
            AppRunner appRunner = new(_outputHelper, Assembly.GetExecutingAssembly(), isWebApp: true);
            appRunner.ConnectionMode = appConnectionMode;
            appRunner.DiagnosticPortPath = diagnosticPortPath;
            appRunner.AdditionalArguments = additionalArguments;

            return appRunner;
        }

        private async Task ApiCallHelper(string hostName, string[] paths, ApiClient client)
        {
            foreach (string path in paths)
            {
                string url = hostName + path;
                _ = await client.ApiCall(url);
            }
        }

        private async Task CaptureTrace(AppRunner runner, ApiClient client)
        {
            int processId = await runner.ProcessIdTask;

            int retryCounter = 0;

            // Repeatedly check if dotnet-monitor is detecting our process; due to timing issues,
            // tests were sometimes failing due to the target process not being found when collecting a trace.
            while (retryCounter < 5)
            {
                _outputHelper.WriteLine("Retry counter: " + retryCounter);

                IEnumerable<ProcessIdentifier> identifiers = await client.GetProcessesWithRetryAsync(
                    _outputHelper,
                    new[] { processId });

                if (identifiers.Any())
                {
                    break;
                }

                retryCounter += 1;
                await Task.Delay(500);
            }

            using ResponseStreamHolder holder = await client.CaptureTraceAsync(processId, TraceDuration, WebApi.Models.TraceProfile.Http);
            Assert.NotNull(holder);

            await TraceTestUtilities.ValidateTrace(holder.Stream);
        }
#endif
    }
}
