// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Graphs;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Fixtures;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.HttpApi;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Runners;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

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

        /// <summary>
        /// Validates that a non-startup rule will complete when it has an action limit specified
        /// without a sliding window duration.
        /// </summary>
        [Theory]
        [InlineData(DiagnosticPortConnectionMode.Listen)]
        public async Task CollectionRuleAndApi_AspNetResponsesStatusTest(DiagnosticPortConnectionMode mode)
        {
            const int ExpectedResponseCount = 2;

            using TemporaryDirectory tempDirectory = new(_outputHelper);
            string ExpectedFilePath = Path.Combine(tempDirectory.FullName, "file.txt");
            string ExpectedFileContent = Guid.NewGuid().ToString("N");

            DiagnosticPortHelper.Generate(
                mode,
                out DiagnosticPortConnectionMode appConnectionMode,
                out string diagnosticPortPath);

            await using MonitorCollectRunner toolRunner = new(_outputHelper);
            toolRunner.ConnectionMode = mode;
            toolRunner.DiagnosticPortPath = diagnosticPortPath;
            toolRunner.DisableAuthentication = true;

            AppRunner appRunner = new(_outputHelper, Assembly.GetExecutingAssembly(), isWebApp: true);
            appRunner.ConnectionMode = appConnectionMode;
            appRunner.DiagnosticPortPath = diagnosticPortPath;
            appRunner.ScenarioName = TestAppScenarios.AsyncWait.Name;

            Task ruleStartedTask = toolRunner.WaitForCollectionRuleActionsCompletedAsync(DefaultRuleName);

            await appRunner.ExecuteNoCommandsAsync(async () =>
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

                try
                {
                    string pathAndQuery = "http://localhost:82";
                    HttpResponseMessage message = await apiClient.ApiCall(pathAndQuery);
                    string pathAndQuery2 = "http://localhost:82/Privacy";
                    HttpResponseMessage message2 = await apiClient.ApiCall(pathAndQuery2);
                    string pathAndQuery3 = "http://localhost:82";
                    HttpResponseMessage message3 = await apiClient.ApiCall(pathAndQuery3);
                }
                catch (ApiStatusCodeException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
                {
                    // Handle cases where it fails to locate the single process.
                }

                await ruleStartedTask;
                Assert.True(ruleStartedTask.IsCompleted);

                Assert.True(File.Exists(ExpectedFilePath));
                Assert.Equal(ExpectedFileContent, File.ReadAllText(ExpectedFilePath));

                //Directory.Delete(ExpectedFilePath);

                ////////////////////////////

                int processId = await appRunner.ProcessIdTask;

                TimeSpan duration = TimeSpan.FromSeconds(5);
                using ResponseStreamHolder holder = await apiClient.CaptureTraceAsync(processId, duration, WebApi.Models.TraceProfile.Http);
                Assert.NotNull(holder);

                await TraceTestUtilities.ValidateTrace(holder.Stream);

                //await appRunner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);

                ////////////////////////////

                Task ruleStartedTask2 = toolRunner.WaitForCollectionRuleActionsCompletedAsync(DefaultRuleName);

                try
                {
                    string pathAndQuery = "http://localhost:82";
                    HttpResponseMessage message = await apiClient.ApiCall(pathAndQuery);
                    string pathAndQuery2 = "http://localhost:82/Privacy";
                    HttpResponseMessage message2 = await apiClient.ApiCall(pathAndQuery2);
                    string pathAndQuery3 = "http://localhost:82";
                    HttpResponseMessage message3 = await apiClient.ApiCall(pathAndQuery3);
                }
                catch (ApiStatusCodeException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
                {
                    // Handle cases where it fails to locate the single process.
                }

                await ruleStartedTask2;
                Assert.True(ruleStartedTask2.IsCompleted);

                Assert.True(File.Exists(ExpectedFilePath));
                Assert.Equal(ExpectedFileContent, File.ReadAllText(ExpectedFilePath));

                appRunner.KillProcess();
            });
        }

        /// <summary>
        /// Validates that a non-startup rule will complete when it has an action limit specified
        /// without a sliding window duration.
        /// </summary>
        [Theory]
        [InlineData(DiagnosticPortConnectionMode.Listen)]
        public async Task CollectionRuleAndApi_AspNetRequestDurationTest(DiagnosticPortConnectionMode mode)
        {
            const int ExpectedRequestCount = 2;

            using TemporaryDirectory tempDirectory = new(_outputHelper);
            string ExpectedFilePath = Path.Combine(tempDirectory.FullName, "file.txt");
            string ExpectedFileContent = Guid.NewGuid().ToString("N");

            DiagnosticPortHelper.Generate(
                mode,
                out DiagnosticPortConnectionMode appConnectionMode,
                out string diagnosticPortPath);

            await using MonitorCollectRunner toolRunner = new(_outputHelper);
            toolRunner.ConnectionMode = mode;
            toolRunner.DiagnosticPortPath = diagnosticPortPath;
            toolRunner.DisableAuthentication = true;

            AppRunner appRunner = new(_outputHelper, Assembly.GetExecutingAssembly(), isWebApp: true);
            appRunner.ConnectionMode = appConnectionMode;
            appRunner.DiagnosticPortPath = diagnosticPortPath;
            appRunner.ScenarioName = TestAppScenarios.AsyncWait.Name;

            Task ruleStartedTask = toolRunner.WaitForCollectionRuleActionsCompletedAsync(DefaultRuleName);

            await appRunner.ExecuteNoCommandsAsync(async () =>
            {
                RootOptions newOptions = new();
                newOptions.CreateCollectionRule(DefaultRuleName)
                        .SetAspNetRequestDurationTrigger(options =>
                        {
                            options.RequestCount = ExpectedRequestCount;
                            options.RequestDuration = TimeSpan.FromSeconds(1);
                        })
                        .AddExecuteActionAppAction("TextFileOutput", ExpectedFilePath, ExpectedFileContent);

                await toolRunner.WriteUserSettingsAsync(newOptions);
                await toolRunner.StartAsync();

                using HttpClient httpClient = await toolRunner.CreateHttpClientDefaultAddressAsync(_httpClientFactory);
                ApiClient apiClient = new(_outputHelper, httpClient);

                try
                {
                    string pathAndQuery = "http://localhost:82/SlowResponse";
                    HttpResponseMessage message = await apiClient.ApiCall(pathAndQuery);
                    HttpResponseMessage message2 = await apiClient.ApiCall(pathAndQuery);
                    HttpResponseMessage message3 = await apiClient.ApiCall(pathAndQuery);
                }
                catch (ApiStatusCodeException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
                {
                    // Handle cases where it fails to locate the single process.
                }

                await ruleStartedTask;
                Assert.True(ruleStartedTask.IsCompleted);

                Assert.True(File.Exists(ExpectedFilePath));
                Assert.Equal(ExpectedFileContent, File.ReadAllText(ExpectedFilePath));

                //Directory.Delete(ExpectedFilePath);

                ////////////////////////////

                int processId = await appRunner.ProcessIdTask;

                TimeSpan duration = TimeSpan.FromSeconds(5);
                using ResponseStreamHolder holder = await apiClient.CaptureTraceAsync(processId, duration, WebApi.Models.TraceProfile.Http);
                Assert.NotNull(holder);

                await TraceTestUtilities.ValidateTrace(holder.Stream);

                //await appRunner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);

                ////////////////////////////

                Task ruleStartedTask2 = toolRunner.WaitForCollectionRuleActionsCompletedAsync(DefaultRuleName);

                try
                {
                    string pathAndQuery = "http://localhost:82/SlowResponse";
                    HttpResponseMessage message = await apiClient.ApiCall(pathAndQuery);
                    HttpResponseMessage message2 = await apiClient.ApiCall(pathAndQuery);
                    HttpResponseMessage message3 = await apiClient.ApiCall(pathAndQuery);
                }
                catch (ApiStatusCodeException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
                {
                    // Handle cases where it fails to locate the single process.
                }

                await ruleStartedTask2;
                Assert.True(ruleStartedTask2.IsCompleted);

                Assert.True(File.Exists(ExpectedFilePath));
                Assert.Equal(ExpectedFileContent, File.ReadAllText(ExpectedFilePath));

                appRunner.KillProcess();
            });
        }

#endif
    }
}
