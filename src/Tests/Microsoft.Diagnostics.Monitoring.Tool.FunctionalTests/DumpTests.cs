// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Fixtures;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.HttpApi;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Runners;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
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
        [InlineData(DiagnosticPortConnectionMode.Connect, DumpType.Full)]
        [InlineData(DiagnosticPortConnectionMode.Connect, DumpType.Mini)]
        [InlineData(DiagnosticPortConnectionMode.Connect, DumpType.Triage)]
        [InlineData(DiagnosticPortConnectionMode.Connect, DumpType.WithHeap)]
        [InlineData(DiagnosticPortConnectionMode.Listen, DumpType.Full)]
        [InlineData(DiagnosticPortConnectionMode.Listen, DumpType.Mini)]
        [InlineData(DiagnosticPortConnectionMode.Listen, DumpType.Triage)]
        [InlineData(DiagnosticPortConnectionMode.Listen, DumpType.WithHeap)]
        public Task DumpTest(DiagnosticPortConnectionMode mode, DumpType type)
        {
            return RetryUtilities.RetryAsync(
                func: () => DumpTestCore(mode, type),
                shouldRetry: (Exception ex) => ex is TaskCanceledException,
                outputHelper: _outputHelper);
        }

        private Task DumpTestCore(DiagnosticPortConnectionMode mode, DumpType type)
        {
            return ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
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
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && TestDotNetHost.RuntimeVersion.Major == 5)
                    {
                        runner.Environment.Add(DumpTestUtilities.EnableElfDumpOnMacOS, "1");
                    }
                },
                configureTool: runner =>
                {
                    string dumpTempFolder = Path.Combine(runner.TempPath, "Dumps");

                    // The dump temp folder should not exist in order to test that capturing dumps into the folder
                    // will work since dotnet-monitor should ensure the folder is created before issuing the dump command.
                    Assert.False(Directory.Exists(dumpTempFolder), "The dump temp folder should not exist.");

                    runner.ConfigurationFromEnvironment.SetDumpTempFolder(dumpTempFolder);
                });
        }
    }
}
