// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.UnitTests.HttpApi;
using Microsoft.Diagnostics.Monitoring.UnitTests.Models;
using Microsoft.Diagnostics.Monitoring.UnitTests.Options;
using Microsoft.Diagnostics.Monitoring.UnitTests.Runners;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.UnitTests
{
    public class ProcessTests
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(1);

        private readonly ITestOutputHelper _outputHelper;

        public ProcessTests(ITestOutputHelper outputHelper)
        {
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

            await Task.Delay(TimeSpan.FromSeconds(1));

            using ApiClient client = new(_outputHelper, await toolRunner.GetDefaultAddressAsync(DefaultTimeout));

            int appProcessId;
            await using (AppRunner appRunner = new(_outputHelper))
            {
                appRunner.ConnectionMode = appConnectionMode;
                appRunner.DiagnosticPortPath = diagnosticPortPath;
                appRunner.ScenarioName = TestAppScenarios.SpinWait.Name;
                await appRunner.StartAsync(DefaultTimeout);

                appProcessId = appRunner.ProcessId;

                await appRunner.SendStartScenarioAsync(DefaultTimeout);

                await VerifyProcessAsync(client, await client.GetProcessesAsync(DefaultTimeout), appProcessId);

                await EndSpinWaitScenarioAsync(appRunner);
            }

            await Task.Delay(TimeSpan.FromSeconds(1));

            // Verify app is no longer reported
            IEnumerable<ProcessIdentifier> identifiers = await client.GetProcessesAsync(DefaultTimeout);
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

            using ApiClient client = new(_outputHelper, await toolRunner.GetDefaultAddressAsync(DefaultTimeout));

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
                    runner.ScenarioName = TestAppScenarios.SpinWait.Name;
                    await runner.StartAsync(DefaultTimeout);

                    await runner.SendStartScenarioAsync(DefaultTimeout);

                    appRunners[i] = runner;
                    processIds[i] = runner.ProcessId;
                }

                await Task.Delay(TimeSpan.FromSeconds(1));

                // Query for process identifiers
                identifiers = await client.GetProcessesAsync(DefaultTimeout);
                Assert.NotNull(identifiers);

                // Verify each app instance is reported and shut them down.
                foreach (AppRunner runner in appRunners)
                {
                    await VerifyProcessAsync(client, identifiers, runner.ProcessId);

                    await EndSpinWaitScenarioAsync(runner);
                }
            }
            finally
            {
                await appRunners.DisposeItemsAsync();
            }

            await Task.Delay(TimeSpan.FromSeconds(1));

            // Query for process identifiers
            identifiers = await client.GetProcessesAsync(DefaultTimeout);
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
        /// Has the SpinWait scenario end.
        /// </summary>
        private static async Task EndSpinWaitScenarioAsync(AppRunner runner)
        {
            await runner.SendCommandAsync(TestAppScenarios.SpinWait.Commands.Continue, DefaultTimeout);

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
