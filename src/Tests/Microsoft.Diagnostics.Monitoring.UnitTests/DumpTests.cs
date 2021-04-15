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
using System.Buffers;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.UnitTests
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
        public async Task DumpTest(DiagnosticPortConnectionMode mode, DumpType type)
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
            
            AppRunner appRunner = new(_outputHelper);
            appRunner.ConnectionMode = appConnectionMode;
            appRunner.DiagnosticPortPath = diagnosticPortPath;
            appRunner.ScenarioName = TestAppScenarios.AsyncWait.Name;

            // MachO not supported, only ELF: https://github.com/dotnet/runtime/blob/main/docs/design/coreclr/botr/xplat-minidump-generation.md#os-x
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                appRunner.Environment.Add("COMPlus_DbgEnableElfDumpOnMacOS", "1");
            }

            try
            {
                await appRunner.ExecuteAsync(async () =>
                {
                    ProcessInfo processInfo = await apiClient.GetProcessAsync(appRunner.ProcessId);
                    Assert.NotNull(processInfo);

                    using ResponseStreamHolder holder = await apiClient.CaptureDumpAsync(appRunner.ProcessId, type);
                    Assert.NotNull(holder);

                    const int bufferLength = 10240; // 10k buffer
                    long total = await ArrayPool<byte>.Shared.RentAndReturnAsync(bufferLength, async buffer =>
                    {
                        using CancellationTokenSource cancellation = new(TestTimeouts.HttpApi);

                        int read;
                        long total = 0;
                        while (0 != (read = await holder.Stream.ReadAsync(buffer, 0, buffer.Length, cancellation.Token)))
                        {
                            total += read;
                        }
                        return total;
                    });

                    _outputHelper.WriteLine("Dump Size: {0} bytes", total);
                    // Dumps should have at least 10k of data (this is an arbitrary threshold).
                    // CONSIDER: Is there a better lightweight way to check if the dump is viable?
                    // e.g. install dotnet-dump and do a brief analysis; or check the header on the
                    // file content.
                    Assert.True(total > 10_000);

                    await appRunner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                });
            }
            catch (ApiStatusCodeException ex) when (IsSegFaultOnOSX(appRunner, ex.StatusCode))
            {
                // Don't fail the test if the test app segfaults due to calling the dump commmand.
                _outputHelper.WriteLine("WARNING: Test app segfaulted while producing dump type '{0}'.", type);
                return;
            }
            Assert.Equal(0, appRunner.ExitCode);
        }

        private static bool IsSegFaultOnOSX(AppRunner runner, HttpStatusCode? statusCode)
        {
            // Requesting a dump of a dotnet process on OSX sometimes causes the process to segfault.
            // It returns an exit code of 139 (128 + 11), which indicates SIGSEGV. This causes the
            // HTTP request to fail (500) because the diagnostics connection is terminated.
            return RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                && runner.ExitCode == 139
                && statusCode.HasValue
                && statusCode.Value == HttpStatusCode.InternalServerError;
        }
    }
}
