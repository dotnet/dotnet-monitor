// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.UnitTests.Fixtures;
using Microsoft.Diagnostics.Monitoring.UnitTests.HttpApi;
using Microsoft.Diagnostics.Monitoring.UnitTests.Models;
using Microsoft.Diagnostics.Monitoring.UnitTests.Options;
using Microsoft.Diagnostics.Monitoring.UnitTests.Runners;
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

namespace Microsoft.Diagnostics.Monitoring.UnitTests
{
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
#if NET5_0_OR_GREATER
        [InlineData(DiagnosticPortConnectionMode.Listen)]
#endif
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
                    await VerifyProcessAsync(client, await client.GetProcessesAsync(), runner.ProcessId, expectedEnvVarValue);

                    await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                },
                postAppValidate: async (client, processId) =>
                {
                    // Verify app is no longer reported
                    IEnumerable<ProcessIdentifier> identifiers = await client.GetProcessesAsync();
                    Assert.NotNull(identifiers);
                    ProcessIdentifier identifier = identifiers.FirstOrDefault(p => p.Pid == processId);
                    Assert.Null(identifier);
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
#if NET5_0_OR_GREATER
        [InlineData(DiagnosticPortConnectionMode.Listen)]
#endif
        public async Task MultiProcessIdentificationTest(DiagnosticPortConnectionMode mode)
        {
            DiagnosticPortHelper.Generate(
                mode,
                out DiagnosticPortConnectionMode appConnectionMode,
                out string diagnosticPortPath);

            await using MonitorRunner toolRunner = new(_outputHelper);
            toolRunner.ConnectionMode = mode;
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

            IEnumerable<ProcessIdentifier> identifiers;
            await appRunners.ExecuteAsync(async () =>
            {
                // Query for process identifiers
                identifiers = await apiClient.GetProcessesAsync();
                Assert.NotNull(identifiers);

                foreach (ProcessIdentifier processIdentifier in identifiers)
                {
                    int pid = processIdentifier.Pid;
                    Guid uid = processIdentifier.Uid;
                    string name = processIdentifier.Name;
#if NET5_0_OR_GREATER
                    // CHECK 1: Get response for processes using PID, UID, and Name and check for consistency

                    List<ProcessInfo> processInfoQueriesCheck1 = new List<ProcessInfo>();

                    processInfoQueriesCheck1.Add(await apiClient.GetProcessAsync(pid: pid, uid: null, name: null));
                    // Only check with uid if it is non-empty; this can happen in connect mode if the ProcessInfo command fails
                    // to respond within the short period of time that is used to get the additional process information.
                    if (uid == Guid.Empty)
                    {
                        _outputHelper.WriteLine("Skipped uid-only check because it is empty GUID.");
                    }
                    else
                    {
                        processInfoQueriesCheck1.Add(await apiClient.GetProcessAsync(pid: null, uid: uid, name: null));
                    }

                    VerifyProcessInfoEquality(processInfoQueriesCheck1);
#endif
                    // CHECK 2: Get response for requests using PID | PID and UID | PID, UID, and Name and check for consistency

                    List<ProcessInfo> processInfoQueriesCheck2 = new List<ProcessInfo>();

                    processInfoQueriesCheck2.Add(await apiClient.GetProcessAsync(pid: pid, uid: null, name: null));
                    processInfoQueriesCheck2.Add(await apiClient.GetProcessAsync(pid: pid, uid: uid, name: null));
                    processInfoQueriesCheck2.Add(await apiClient.GetProcessAsync(pid: pid, uid: uid, name: name));

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

                    await VerifyProcessAsync(apiClient, identifiers, runner.ProcessId, expectedEnvVarValue);

                    await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                }
            });

            for (int i = 0; i < appCount; i++)
            {
                Assert.True(0 == appRunners[i].ExitCode, $"App {i} exit code is non-zero.");
            }

            // Query for process identifiers
            identifiers = await apiClient.GetProcessesAsync();
            Assert.NotNull(identifiers);

            // Verify none of the apps are reported
            for (int i = 0; i < appCount; i++)
            {
                Assert.Null(identifiers.FirstOrDefault(p => p.Pid == appRunners[i].ProcessId));
            }
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
        private static async Task VerifyProcessAsync(ApiClient client, IEnumerable<ProcessIdentifier> identifiers, int processId, string expectedEnvVarValue)
        {
            Assert.NotNull(identifiers);
            ProcessIdentifier identifier = identifiers.FirstOrDefault(p => p.Pid == processId);
            Assert.NotNull(identifier);

            ProcessInfo info = await client.GetProcessAsync(identifier.Pid);
            Assert.NotNull(info);
            Assert.Equal(identifier.Pid, info.Pid);

#if NET5_0_OR_GREATER
            // Currently, the runtime instance identifier is only provided for .NET 5 and higher
            info = await client.GetProcessAsync(identifier.Uid);
            Assert.NotNull(info);
            Assert.Equal(identifier.Pid, info.Pid);
            Assert.Equal(identifier.Uid, info.Uid);

            Dictionary<string, string> env = await client.GetProcessEnvironmentAsync(processId);
            Assert.NotNull(env);
            Assert.NotEmpty(env);
            Assert.True(env.TryGetValue(ExpectedEnvVarName, out string actualEnvVarValue));
            Assert.Equal(expectedEnvVarValue, actualEnvVarValue);
#else
            // .NET Core 3.1 and earlier do not support getting the environment block
            ValidationProblemDetailsException validationProblemDetailsException = await Assert.ThrowsAsync<ValidationProblemDetailsException>(
                () => client.GetProcessEnvironmentAsync(processId));
            Assert.Equal(HttpStatusCode.BadRequest, validationProblemDetailsException.StatusCode);
            Assert.Equal(StatusCodes.Status400BadRequest, validationProblemDetailsException.Details.Status);
#endif
        }
    }
}
