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
using Microsoft.FileFormats;
using Microsoft.FileFormats.ELF;
using System.IO;
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

            await appRunner.ExecuteAsync(async () =>
            {
                ProcessInfo processInfo = await apiClient.GetProcessAsync(appRunner.ProcessId);
                Assert.NotNull(processInfo);

                using ResponseStreamHolder holder = await apiClient.CaptureDumpAsync(appRunner.ProcessId, type);
                Assert.NotNull(holder);

                byte[] headerBuffer = new byte[64];

                // Read enough to deserialize the header.
                int read;
                int total = 0;
                using CancellationTokenSource cancellation = new(TestTimeouts.HttpApi);
                while (total < headerBuffer.Length && 0 != (read = await holder.Stream.ReadAsync(headerBuffer, total, headerBuffer.Length - total, cancellation.Token)))
                {
                    total += read;
                }
                Assert.Equal(headerBuffer.Length, total);

                // Read header and validate
                using MemoryStream headerStream = new(headerBuffer);

                StreamAddressSpace dumpAddressSpace = new(headerStream);
                Reader dumpReader = new(dumpAddressSpace);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    MinidumpHeader header = dumpReader.Read<MinidumpHeader>(0);
                    // Validate Signature
                    Assert.True(header.IsSignatureValid.Check());
                }
                else
                {
                    ELFHeader header = dumpReader.Read<ELFHeader>(0);
                    // Validate Signature
                    Assert.True(header.IsIdentMagicValid.Check());
                    // Validate ELF file is a core dump
                    Assert.Equal(ELFHeaderType.Core, header.Type);
                }

                await appRunner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
            });
            Assert.Equal(0, appRunner.ExitCode);
        }

        private class MinidumpHeader : TStruct
        {
            public uint Signature = 0;
            public uint Version = 0;
            public uint NumberOfStreams = 0;
            public uint StreamDirectoryRva = 0;
            public uint CheckSum = 0;
            public uint TimeDateStamp = 0;
            public ulong Flags = 0;

            // 50,4D,44,4D = PMDM
            public ValidationRule IsSignatureValid => new ValidationRule("Invalid Signature", () => Signature == 0x504D444DU);
        }
    }
}
