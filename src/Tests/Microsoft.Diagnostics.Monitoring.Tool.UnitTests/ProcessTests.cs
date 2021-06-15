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
        public async Task SingleProcessIdentificationTest(DiagnosticPortConnectionMode mode)
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

            AppRunner appRunner = new(_outputHelper, Assembly.GetExecutingAssembly());
            appRunner.ConnectionMode = appConnectionMode;
            appRunner.DiagnosticPortPath = diagnosticPortPath;
            appRunner.ScenarioName = TestAppScenarios.AsyncWait.Name;

            string expectedEnvVarValue = Guid.NewGuid().ToString("D");
            appRunner.Environment[ExpectedEnvVarName] = expectedEnvVarValue;

            await appRunner.ExecuteAsync(async () =>
            {
                await VerifyProcessAsync(apiClient, await apiClient.GetProcessesAsync(), appRunner.ProcessId, expectedEnvVarValue);

                await appRunner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
            });
            Assert.Equal(0, appRunner.ExitCode);

            // Verify app is no longer reported
            IEnumerable<ProcessIdentifier> identifiers = await apiClient.GetProcessesAsync();
            Assert.NotNull(identifiers);
            ProcessIdentifier identifier = identifiers.FirstOrDefault(p => p.Pid == appRunner.ProcessId);
            Assert.Null(identifier);
        }

        /// <summary>
        /// Tests that multiple processes are discoverable by dotnet-monitor.
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
        /// Verifies that a process was found in the identifiers list and checks the /processes/{processKey} route for the same process.
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
