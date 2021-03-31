// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.UnitTests.Fixtures;
using Microsoft.Diagnostics.Monitoring.UnitTests.HttpApi;
using Microsoft.Diagnostics.Monitoring.UnitTests.Models;
using Microsoft.Diagnostics.Monitoring.UnitTests.Options;
using Microsoft.Diagnostics.Monitoring.UnitTests.Runners;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.UnitTests
{
    [Collection(DefaultCollectionFixture.Name)]
    public class ProcessTests
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);
        private static readonly TimeSpan ExceptionTimeout = TimeSpan.FromSeconds(5);

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITestOutputHelper _outputHelper;

        public ProcessTests(ITestOutputHelper outputHelper, ServiceProviderFixture serviceProviderFixture)
        {
            _httpClientFactory = serviceProviderFixture.ServiceProvider.GetService<IHttpClientFactory>();
            _outputHelper = outputHelper;
        }

        /// <summary>
        /// Tests that a single process is discoverable by dotnet-monitor in connect mode.
        /// </summary>
        [Fact]
        public Task SingleProcessConnectModeTest()
        {
            return SingleProcessCore(DiagnosticPortConnectionMode.Connect);
        }

#if NET5_0_OR_GREATER
        /// <summary>
        /// Tests that a single process is discoverable by dotnet-monitor in listen mode.
        /// </summary>
        [Fact]
        public Task SingleProcessListenModeTest()
        {
            return SingleProcessCore(DiagnosticPortConnectionMode.Listen);
        }
#endif

        /// <summary>
        /// Tests that a single process is discoverable by dotnet-monitor.
        /// </summary>
        private async Task SingleProcessCore(DiagnosticPortConnectionMode monitorConnectionMode)
        {
            GenerateDiagnosticPortInfo(
                monitorConnectionMode,
                out DiagnosticPortConnectionMode appConnectionMode,
                out string diagnosticPortPath);

            await using MonitorRunner toolRunner = new(_outputHelper);
            toolRunner.ConnectionMode = monitorConnectionMode;
            toolRunner.DiagnosticPortPath = diagnosticPortPath;
            toolRunner.DisableAuthentication = true;
            await toolRunner.StartAsync(DefaultTimeout);

            using HttpClient httpClient = await toolRunner.CreateHttpClientDefaultAddressAsync(_httpClientFactory, DefaultTimeout);
            ApiClient apiClient = new(_outputHelper, httpClient);

            int appProcessId;
            AppRunner appRunner = new(_outputHelper);
            try
            {
                appRunner.ConnectionMode = appConnectionMode;
                appRunner.DiagnosticPortPath = diagnosticPortPath;
                appRunner.ScenarioName = TestAppScenarios.AsyncWait.Name;
                await appRunner.StartAsync(DefaultTimeout);

                appProcessId = appRunner.ProcessId;

                await appRunner.SendStartScenarioAsync(DefaultTimeout);

                await VerifyProcessAsync(apiClient, await apiClient.GetProcessesAsync(DefaultTimeout), appProcessId);

                await EndAsyncWaitScenarioAsync(appRunner);

                // This gives the app time to send out any remaining stdout/stderr messages,
                // exit properly, and delete its diagnostic pipe.
                await appRunner.WaitForExitAsync(DefaultTimeout);
            }
            catch (Exception)
            {
                // If an exception is thrown, give app some time to send out any remaining
                // stdout/stderr messages.
                await Task.Delay(ExceptionTimeout);

                throw;
            }
            finally
            {
                await appRunner.DisposeAsync();
            }

            // Verify app is no longer reported
            IEnumerable<ProcessIdentifier> identifiers = await apiClient.GetProcessesAsync(DefaultTimeout);
            Assert.NotNull(identifiers);
            ProcessIdentifier identifier = identifiers.FirstOrDefault(p => p.Pid == appProcessId);
            Assert.Null(identifier);
        }

        /// <summary>
        /// Tests that multiple processes are discoverable by dotnet-monitor in connect mode.
        /// </summary>
        [Fact]
        public Task MultiProcessConnectModeTest()
        {
            return MultiProcessCore(DiagnosticPortConnectionMode.Connect);
        }

#if NET5_0_OR_GREATER
        /// <summary>
        /// Tests that multiple processes are discoverable by dotnet-monitor in listen mode.
        /// </summary>
        [Fact]
        public Task MultiProcessListenModeTest()
        {
            return MultiProcessCore(DiagnosticPortConnectionMode.Listen);
        }
#endif

        /// <summary>
        /// Tests that multiple processes are discoverable by dotnet-monitor.
        /// </summary>
        private async Task MultiProcessCore(DiagnosticPortConnectionMode monitorConnectionMode)
        {
            GenerateDiagnosticPortInfo(
                monitorConnectionMode,
                out DiagnosticPortConnectionMode appConnectionMode,
                out string diagnosticPortPath);

            await using MonitorRunner toolRunner = new(_outputHelper);
            toolRunner.ConnectionMode = monitorConnectionMode;
            toolRunner.DiagnosticPortPath = diagnosticPortPath;
            toolRunner.DisableAuthentication = true;
            await toolRunner.StartAsync(DefaultTimeout);

            using HttpClient httpClient = await toolRunner.CreateHttpClientDefaultAddressAsync(_httpClientFactory, DefaultTimeout);
            ApiClient apiClient = new(_outputHelper, httpClient);

            const int appCount = 3;
            int[] processIds = new int[appCount];
            AppRunner[] appRunners = new AppRunner[appCount];
            IEnumerable<ProcessIdentifier> identifiers;

            try
            {
                // Start several apps
                foreach (int i in Enumerable.Range(0, appCount))
                {
                    AppRunner runner = new(_outputHelper, appId: i + 1);
                    runner.ConnectionMode = appConnectionMode;
                    runner.DiagnosticPortPath = diagnosticPortPath;
                    runner.ScenarioName = TestAppScenarios.AsyncWait.Name;
                    await runner.StartAsync(DefaultTimeout);

                    await runner.SendStartScenarioAsync(DefaultTimeout);

                    appRunners[i] = runner;
                    processIds[i] = runner.ProcessId;
                }

                // Query for process identifiers
                identifiers = await apiClient.GetProcessesAsync(DefaultTimeout);
                Assert.NotNull(identifiers);

                // Verify each app instance is reported and shut them down.
                foreach (AppRunner runner in appRunners)
                {
                    await VerifyProcessAsync(apiClient, identifiers, runner.ProcessId);
                }

                // This gives apps time to send out any remaining stdout/stderr messages,
                // exit properly, and delete their diagnostic pipes.
                await Task.WhenAll(appRunners.Select(async runner =>
                    {
                        await EndAsyncWaitScenarioAsync(runner);

                        await runner.WaitForExitAsync(DefaultTimeout);
                    }));
            }
            catch (Exception)
            {
                // If an exception is thrown, give apps some time to send out any remaining
                // stdout/stderr messages.
                await Task.Delay(ExceptionTimeout);

                throw;
            }
            finally
            {
                await appRunners.DisposeItemsAsync();
            }

            // Query for process identifiers
            identifiers = await apiClient.GetProcessesAsync(DefaultTimeout);
            Assert.NotNull(identifiers);

            // Verify none of the apps are reported
            for (int i = 0; i < appCount; i++)
            {
                Assert.Null(identifiers.FirstOrDefault(p => p.Pid == processIds[i]));
            }
        }

        /// <summary>
        /// Verifies that a process was found in the identifiers list and checks the /processes/{processKey} route for the same process.
        /// </summary>
        private static async Task VerifyProcessAsync(ApiClient client, IEnumerable<ProcessIdentifier> identifiers, int processId)
        {
            Assert.NotNull(identifiers);
            ProcessIdentifier identifier = identifiers.FirstOrDefault(p => p.Pid == processId);
            Assert.NotNull(identifier);

            ProcessInfo info = await client.GetProcessAsync(identifier.Pid, DefaultTimeout);
            Assert.NotNull(info);
            Assert.Equal(identifier.Pid, info.Pid);

#if NET5_0_OR_GREATER
            // Currently, the runtime instance identifier is only provided for .NET 5 and higher
            info = await client.GetProcessAsync(identifier.Uid, DefaultTimeout);
            Assert.NotNull(info);
            Assert.Equal(identifier.Pid, info.Pid);
            Assert.Equal(identifier.Uid, info.Uid);
#endif
        }

        /// <summary>
        /// Have the AsyncWait scenario end.
        /// </summary>
        private static async Task EndAsyncWaitScenarioAsync(AppRunner runner)
        {
            await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue, DefaultTimeout);

            await runner.SendEndScenarioAsync(DefaultTimeout);
        }

        /// <summary>
        /// Calculates the app's diagnostic port mode and generates a port path
        /// if <paramref name="monitorConnectionMode"/> is <see cref="DiagnosticPortConnectionMode.Listen"/>.
        /// </summary>
        private static void GenerateDiagnosticPortInfo(
            DiagnosticPortConnectionMode monitorConnectionMode,
            out DiagnosticPortConnectionMode appConnectionMode,
            out string diagnosticPortPath)
        {
            appConnectionMode = DiagnosticPortConnectionMode.Listen;
            diagnosticPortPath = null;

            if (DiagnosticPortConnectionMode.Listen == monitorConnectionMode)
            {
                appConnectionMode = DiagnosticPortConnectionMode.Connect;

                string fileName = Guid.NewGuid().ToString("D");
                diagnosticPortPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                    fileName : Path.Combine(Path.GetTempPath(), fileName);
            }
        }
    }
}
