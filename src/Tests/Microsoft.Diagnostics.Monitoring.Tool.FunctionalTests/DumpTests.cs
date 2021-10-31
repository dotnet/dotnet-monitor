// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests
{
    [Collection(DefaultCollectionFixture.Name)]
    public class DumpTests
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITestOutputHelper _outputHelper;

        public DumpTests(ITestOutputHelper outputHelper, ServiceProviderFixture serviceProviderFixture)
        {
            _httpClientFactory = serviceProviderFixture.ServiceProvider.GetService<IHttpClientFactory>();
            _outputHelper = outputHelper;
        }

        [Theory]
        [MemberData(nameof(GetTestParameters), MemberType = typeof(DumpTests))]
        public Task DumpTest(TargetFrameworkMoniker appTfm, DiagnosticPortConnectionMode mode, DumpType type)
        {
            return ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                appTfm,
                mode,
                TestAppScenarios.AsyncWait.Name,
                appValidate: async (runner, client) =>
                {
                    int processId = await runner.ProcessIdTask;

                    using ResponseStreamHolder holder = await client.CaptureDumpAsync(processId, type);
                    Assert.NotNull(holder);

                    // The dump operation may still be in progress but the process should still be discoverable.
                    // If this check fails, then the dump operation is causing dotnet-monitor to not be able
                    // to observe the process any more.
                    ProcessInfo processInfo = await client.GetProcessAsync(processId);
                    Assert.NotNull(processInfo);

                    await DumpTestUtilities.ValidateDump(runner.Environment.ContainsKey(DumpTestUtilities.EnableElfDumpOnMacOS), holder.Stream);

                    await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                },
                configureApp: runner =>
                {
                    // MachO not supported on .NET 5, only ELF: https://github.com/dotnet/runtime/blob/main/docs/design/coreclr/botr/xplat-minidump-generation.md#os-x
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && appTfm == TargetFrameworkMoniker.Net50)
                    {
                        runner.Environment.Add(DumpTestUtilities.EnableElfDumpOnMacOS, "1");
                    }
                });
        }

        public static IEnumerable<object[]> GetTestParameters()
        {
            foreach (Tuple<TargetFrameworkMoniker, DiagnosticPortConnectionMode> tuple in CommonMemberDataParameters.AllTfmsAndConnectionModes)
            {
                TargetFrameworkMoniker appTfm = tuple.Item1;
                DiagnosticPortConnectionMode connectionMode = tuple.Item2;

                // MacOS supported dumps starting in .NET 5
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && appTfm.IsLowerThan(TargetFrameworkMoniker.Net50))
                    continue;

                // MacOS dumps inconsistently segfault the runtime on .NET 5: https://github.com/dotnet/dotnet-monitor/issues/174
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && appTfm == TargetFrameworkMoniker.Net50)
                    continue;

                // There is no technical reason for this split. It's more practical so that there
                // is good coverage of the different TFM x ConnectionMode x DumpType combinations
                // without having to test every single combination.
                if (DiagnosticPortConnectionMode.Connect == connectionMode)
                {
                    yield return new object[] { appTfm, connectionMode, DumpType.Full };
                    yield return new object[] { appTfm, connectionMode, DumpType.Mini };
                }
                else
                {
                    yield return new object[] { appTfm, connectionMode, DumpType.Triage };
                    yield return new object[] { appTfm, connectionMode, DumpType.WithHeap };
                }
            }
        }
    }
}
