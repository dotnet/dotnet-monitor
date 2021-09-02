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
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests
{
    [Collection(DefaultCollectionFixture.Name)]
    public class DumpTests : IDumpTestInterface
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITestOutputHelper _outputHelper;

        public DumpTests(ITestOutputHelper outputHelper, ServiceProviderFixture serviceProviderFixture)
        {
            _httpClientFactory = serviceProviderFixture.ServiceProvider.GetService<IHttpClientFactory>();
            _outputHelper = outputHelper;
        }

        [ConditionalTheory(typeof(TestConditions), nameof(TestConditions.IsDumpSupported))]
        [InlineData(DiagnosticPortConnectionMode.Connect, DumpType.Full)]
        [InlineData(DiagnosticPortConnectionMode.Connect, DumpType.Mini)]
        [InlineData(DiagnosticPortConnectionMode.Connect, DumpType.Triage)]
        [InlineData(DiagnosticPortConnectionMode.Connect, DumpType.WithHeap)]
#if NET5_0_OR_GREATER
        [InlineData(DiagnosticPortConnectionMode.Listen, DumpType.Full)]
        [InlineData(DiagnosticPortConnectionMode.Listen, DumpType.Mini)]
        [InlineData(DiagnosticPortConnectionMode.Listen, DumpType.Triage)]
        [InlineData(DiagnosticPortConnectionMode.Listen, DumpType.WithHeap)]
#endif
        public Task DumpTest(DiagnosticPortConnectionMode mode, DumpType type)
        {
            return ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                mode,
                TestAppScenarios.AsyncWait.Name,
                appValidate: async (runner, client) =>
                {
                    ProcessInfo processInfo = await client.GetProcessAsync(runner.ProcessId);
                    Assert.NotNull(processInfo);

                    using ResponseStreamHolder holder = await client.CaptureDumpAsync(runner.ProcessId, type);
                    Assert.NotNull(holder);

                    await IDumpTestInterface.ValidateDump(runner, holder.Stream);

                    await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                },
                configureApp: runner =>
                {
                    // MachO not supported on .NET 5, only ELF: https://github.com/dotnet/runtime/blob/main/docs/design/coreclr/botr/xplat-minidump-generation.md#os-x
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && DotNetHost.RuntimeVersion.Major == 5)
                    {
                        runner.Environment.Add(IDumpTestInterface.EnableElfDumpOnMacOS, "1");
                    }
                });
        }
    }
}
