// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Fixtures;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.HttpApi;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Runners;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    [Collection(DefaultCollectionFixture.Name)]
    public class ProcessTests
    {
        const string ExpectedEnvVarName = "DotnetMonitorTestEnvVar";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITestOutputHelper _outputHelper;

        public ProcessTests(ITestOutputHelper outputHelper, ServiceProviderFixture serviceProviderFixture)
        {
            _httpClientFactory = serviceProviderFixture.ServiceProvider.GetService<IHttpClientFactory>();
            _outputHelper = outputHelper;
        }

        /// <summary>
        /// Tests that a single process is discoverable by dotnet-monitor.
        /// </summary>
        [Theory]
        [InlineData(DiagnosticPortConnectionMode.Connect)]
        [InlineData(DiagnosticPortConnectionMode.Listen)]
        public Task SingleProcessIdentificationTest(DiagnosticPortConnectionMode mode)
        {
            string expectedEnvVarValue = Guid.NewGuid().ToString("D");

            return ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                mode,
                TestAppScenarios.AsyncWait.Name,
                appValidate: async (runner, client) =>
                {
                    int processId = await runner.ProcessIdTask;

                    // GET /processes and filter to just the single process
                    IEnumerable<ProcessIdentifier> identifiers = await client.GetProcessesWithRetryAsync(
                        _outputHelper,
                        new[] { processId });
                    Assert.NotNull(identifiers);
                    Assert.Single(identifiers);

                    await VerifyProcessAsync(client, identifiers, processId, expectedEnvVarValue);

                    await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                },
                postAppValidate: async (client, processId) =>
                {
                    await RetryUtilities.RetryAsync(
                        func: async () =>
                        {
                            await Task.Delay(500);
                            // GET /processes and filter to just the single process
                            IEnumerable<ProcessIdentifier> identifiers = await client.GetProcessesWithRetryAsync(
                                _outputHelper,
                                new[] { processId });

                            // Verify app is no longer reported
                            Assert.NotNull(identifiers);
                            Assert.Empty(identifiers);
                        },
                        shouldRetry: (Exception ex) => ex is XunitException,
                        outputHelper: _outputHelper);

                },
                configureApp: runner =>
                {
                    runner.Environment[ExpectedEnvVarName] = expectedEnvVarValue;
                });
        }

        /// <summary>
        /// Tests that multiple processes are discoverable by dotnet-monitor.
        /// Also tests for correct behavior in response to queries with different/multiple process identifiers.
        /// </summary>
        [Theory]
        [InlineData(DiagnosticPortConnectionMode.Connect)]
        [InlineData(DiagnosticPortConnectionMode.Listen)]
        public async Task MultiProcessIdentificationTest(DiagnosticPortConnectionMode mode)
        {
            DiagnosticPortHelper.Generate(
                mode,
                out DiagnosticPortConnectionMode appConnectionMode,
                out string diagnosticPortPath);

            await using MonitorCollectRunner toolRunner = new(_outputHelper);
            toolRunner.ConnectionModeViaCommandLine = mode;
            toolRunner.DiagnosticPortPath = diagnosticPortPath;
            toolRunner.DisableAuthentication = true;
            await toolRunner.StartAsync();

            using HttpClient httpClient = await toolRunner.CreateHttpClientDefaultAddressAsync(_httpClientFactory);
            ApiClient apiClient = new(_outputHelper, httpClient);

            const int appCount = 3;
            AppRunner[] appRunners = new AppRunner[appCount];

            for (int i = 0; i < appCount; i++)
            {
                AppRunner runner = new(_outputHelper, Assembly.GetExecutingAssembly(), appId: i + 1);
                runner.ConnectionMode = appConnectionMode;
                runner.DiagnosticPortPath = diagnosticPortPath;
                runner.ScenarioName = TestAppScenarios.AsyncWait.Name;
                runner.Environment[ExpectedEnvVarName] = Guid.NewGuid().ToString("D");
                appRunners[i] = runner;
            }

            await using IAsyncDisposable _ = appRunners.CreateItemDisposer();

            IList<ProcessIdentifier> identifiers;
            await appRunners.ExecuteAsync(async () =>
            {
                // Scope to only the processes that were launched by the test
                IList<int> unmatchedPids = new List<int>();
                foreach (AppRunner runner in appRunners)
                {
                    unmatchedPids.Add(await runner.ProcessIdTask);
                }

                // Query for process identifiers
                identifiers = (await apiClient.GetProcessesWithRetryAsync(
                    _outputHelper,
                    unmatchedPids.ToArray())).ToList();
                Assert.NotNull(identifiers);

                _outputHelper.WriteLine("Start enumerating discovered processes.");
                foreach (ProcessIdentifier identifier in identifiers.ToList())
                {
                    _outputHelper.WriteLine($"- PID:  {identifier.Pid}");
                    _outputHelper.WriteLine($"  UID:  {identifier.Uid}");
                    _outputHelper.WriteLine($"  Name: {identifier.Name}");

                    unmatchedPids.Remove(identifier.Pid);
                }
                _outputHelper.WriteLine("End enumerating discovered processes");

                Assert.Empty(unmatchedPids);
                Assert.Equal(appRunners.Length, identifiers.Count);

                foreach (ProcessIdentifier processIdentifier in identifiers)
                {
                    int pid = processIdentifier.Pid;
                    Guid uid = processIdentifier.Uid;
                    string name = processIdentifier.Name;

                    // CHECK 1: Get response for processes using PID, UID, and Name and check for consistency

                    List<ProcessInfo> processInfoQueriesCheck1 = new List<ProcessInfo>();

                    processInfoQueriesCheck1.Add(await apiClient.GetProcessWithRetryAsync(_outputHelper, pid: pid));
                    // Only check with uid if it is non-empty; this can happen in connect mode if the ProcessInfo command fails
                    // to respond within the short period of time that is used to get the additional process information.
                    if (uid == Guid.Empty)
                    {
                        _outputHelper.WriteLine("Skipped uid-only check because it is empty GUID.");
                    }
                    else
                    {
                        processInfoQueriesCheck1.Add(await apiClient.GetProcessWithRetryAsync(_outputHelper, uid: uid));
                    }

                    VerifyProcessInfoEquality(processInfoQueriesCheck1);

                    // CHECK 2: Get response for requests using PID | PID and UID | PID, UID, and Name and check for consistency

                    List<ProcessInfo> processInfoQueriesCheck2 = new List<ProcessInfo>();

                    processInfoQueriesCheck2.Add(await apiClient.GetProcessWithRetryAsync(_outputHelper, pid: pid));
                    processInfoQueriesCheck2.Add(await apiClient.GetProcessWithRetryAsync(_outputHelper, pid: pid, uid: uid));
                    processInfoQueriesCheck2.Add(await apiClient.GetProcessWithRetryAsync(_outputHelper, pid: pid, uid: uid, name: name));

                    VerifyProcessInfoEquality(processInfoQueriesCheck2);

                    // CHECK 3: Get response for processes using PID and an unassociated (randomly generated) UID and ensure the proper exception is thrown

                    await VerifyInvalidRequestException(apiClient, pid, Guid.NewGuid(), null);
                }

                // CHECK 4: Get response for processes using invalid PID, UID, or Name and ensure the proper exception is thrown

                await VerifyInvalidRequestException(apiClient, -1, null, null);
                await VerifyInvalidRequestException(apiClient, null, Guid.NewGuid(), null);
                await VerifyInvalidRequestException(apiClient, null, null, "");

                // Verify each app instance is reported and shut them down.
                foreach (AppRunner runner in appRunners)
                {
                    Assert.True(runner.Environment.TryGetValue(ExpectedEnvVarName, out string expectedEnvVarValue));

                    await VerifyProcessAsync(apiClient, identifiers, await runner.ProcessIdTask, expectedEnvVarValue);

                    await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                }
            });

            /* TESTFIX - Race condition on process termination and user-mode app processing of it.
            for (int i = 0; i < appCount; i++)
            {
                Assert.True(0 == appRunners[i].ExitCode, $"App {i} exit code is non-zero.");
            }

            // Query for process identifiers
            identifiers = (await apiClient.GetProcessesAsync()).ToList();
            Assert.NotNull(identifiers);

            // Verify none of the apps are reported
            List<int> runnerProcessIds = new(appCount);
            for (int i = 0; i < appCount; i++)
            {
                runnerProcessIds.Add(await appRunners[i].ProcessIdTask);
            }

            foreach (ProcessIdentifier identifier in identifiers)
            {
                Assert.DoesNotContain(identifier.Pid, runnerProcessIds);
            }
            */
        }

        /// <summary>
        /// Verifies that each provided instance of ProcessInfo is equivalent in terms of PID, UID, and Name.
        /// </summary>
        private void VerifyProcessInfoEquality(List<ProcessInfo> processInfos)
        {
            List<int> processInfoPIDs = new List<int>();
            List<Guid> processInfoUIDs = new List<Guid>();
            List<string> processInfoNames = new List<string>();

            _outputHelper.WriteLine("Start enumerating collected process information.");

            foreach (ProcessInfo processInfo in processInfos)
            {
                _outputHelper.WriteLine($"- PID:  {processInfo.Pid}");
                _outputHelper.WriteLine($"  UID:  {processInfo.Uid}");
                _outputHelper.WriteLine($"  Name: {processInfo.Name}");

                processInfoPIDs.Add(processInfo.Pid);
                processInfoUIDs.Add(processInfo.Uid);
                processInfoNames.Add(processInfo.Name);
            }

            _outputHelper.WriteLine("End enumerating collected process information.");

            Assert.Single(processInfoPIDs.Distinct());
            Assert.Single(processInfoUIDs.Distinct());
            Assert.Single(processInfoNames.Distinct(StringComparer.Ordinal));
        }

        /// <summary>
        /// Verifies that an invalid Process request throws the correct exception (ValidationProblemDetailsException) and has the correct Status and StatusCode.
        /// </summary>
        private static async Task VerifyInvalidRequestException(ApiClient client, int? pid, Guid? uid, string name)
        {
            ValidationProblemDetailsException validationProblemDetailsException = await Assert.ThrowsAsync<ValidationProblemDetailsException>(
                () => client.GetProcessAsync(pid: pid, uid: uid, name: name));
            Assert.Equal(HttpStatusCode.BadRequest, validationProblemDetailsException.StatusCode);
            Assert.Equal(StatusCodes.Status400BadRequest, validationProblemDetailsException.Details.Status);
        }

        /// <summary>
        /// Verifies that a process was found in the identifiers list and checks the /process?pid={processKey} route for the same process.
        /// </summary>
        private async Task VerifyProcessAsync(ApiClient client, IEnumerable<ProcessIdentifier> identifiers, int processId, string expectedEnvVarValue)
        {
            Assert.NotNull(identifiers);
            ProcessIdentifier identifier = identifiers.FirstOrDefault(p => p.Pid == processId);
            Assert.NotNull(identifier);

            ProcessInfo info = await client.GetProcessWithRetryAsync(_outputHelper, pid: identifier.Pid);
            Assert.NotNull(info);
            Assert.Equal(identifier.Pid, info.Pid);

            // Currently, the runtime instance identifier is only provided for .NET 5 and higher
            info = await client.GetProcessWithRetryAsync(_outputHelper, uid: identifier.Uid);
            Assert.NotNull(info);
            Assert.Equal(identifier.Pid, info.Pid);
            Assert.Equal(identifier.Uid, info.Uid);

            Dictionary<string, string> env = await client.GetProcessEnvironmentAsync(processId);
            Assert.NotNull(env);
            Assert.NotEmpty(env);
            Assert.True(env.TryGetValue(ExpectedEnvVarName, out string actualEnvVarValue));
            Assert.Equal(expectedEnvVarValue, actualEnvVarValue);
        }
    }
}
