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
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Xml.Linq;
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
#if !NET6_0_OR_GREATER
            // Capturing non-full dumps via diagnostic command works inconsistently
            // on Alpine for .NET 5 and lower (the dump command will return successfully, but)
            // the dump file will not exist). Only test other dump types on .NET 6+
            if (DistroInformation.IsAlpineLinux && type != DumpType.Full)
            {
                _outputHelper.WriteLine("Skipped on Alpine for .NET 5 and lower.");
                return Task.CompletedTask;
            }
#endif

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
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && DotNetHost.RuntimeVersion.Major == 5)
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

        [ConditionalTheory(typeof(TestConditions), nameof(TestConditions.IsDumpSupported))]
        [InlineData(DiagnosticPortConnectionMode.Connect, DumpType.Mini)]
        public Task DiagSessionTest(DiagnosticPortConnectionMode mode, DumpType type)
        {
            return ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                mode,
                TestAppScenarios.AsyncWait.Name,
                appValidate: async (runner, client) =>
                {
                    int processId = await runner.ProcessIdTask;

                    PackageMode packageMode = PackageMode.DiagSession;

                    using ResponseStreamHolder holder = await client.CaptureDumpAsync(processId, type, packageMode);
                    Assert.NotNull(holder);

                    // The dump operation may still be in progress but the process should still be discoverable.
                    // If this check fails, then the dump operation is causing dotnet-monitor to not be able
                    // to observe the process any more.
                    ProcessInfo processInfo = await client.GetProcessAsync(processId);
                    Assert.NotNull(processInfo);

                    string tempDiagSessionPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

                    using var fileStream = new FileStream(tempDiagSessionPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.DeleteOnClose);
                    await holder.Stream.CopyToAsync(fileStream);
                    fileStream.Position = 0L;

                    //Validate here
                    using Package package = Package.Open(fileStream, FileMode.Open, FileAccess.Read);

                    //2 parts:
                    //Metadatata
                    //dmp

                    const int expectedParts = 2;

                    PackagePartCollection parts = package.GetParts();
                    Assert.Equal(expectedParts, parts.Count());

                    PackagePart metadata = parts.FirstOrDefault(p => p.Uri.ToString().Contains("metadata.xml"));
                    Assert.NotNull(metadata);
                    PackagePart dump = parts.FirstOrDefault(p => p.Uri.ToString().EndsWith(".dmp"));
                    Assert.NotNull(dump);

                    //TODO Also do verification on metadata file here

                    await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                },
                configureApp: runner =>
                {
                    // MachO not supported on .NET 5, only ELF: https://github.com/dotnet/runtime/blob/main/docs/design/coreclr/botr/xplat-minidump-generation.md#os-x
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && DotNetHost.RuntimeVersion.Major == 5)
                    {
                        runner.Environment.Add(DumpTestUtilities.EnableElfDumpOnMacOS, "1");
                    }
                });
        }
    }
}
