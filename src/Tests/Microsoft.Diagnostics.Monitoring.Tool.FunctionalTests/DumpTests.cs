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

        /*
        public static async Task ValidateDump(AppRunner runner, Stream dumpStream)
        {
            Assert.NotNull(dumpStream);

            byte[] headerBuffer = new byte[64];

            // Read enough to deserialize the header.
            int read;
            int total = 0;
            using CancellationTokenSource cancellation = new(TestTimeouts.DumpTimeout);
            while (total < headerBuffer.Length && 0 != (read = await dumpStream.ReadAsync(headerBuffer, total, headerBuffer.Length - total, cancellation.Token)))
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
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                ELFHeaderIdent ident = dumpReader.Read<ELFHeaderIdent>(0);
                Assert.True(ident.IsIdentMagicValid.Check());
                Assert.True(ident.IsClassValid.Check());
                Assert.True(ident.IsDataValid.Check());

                LayoutManager layoutManager = new();
                layoutManager.AddELFTypes(
                    isBigEndian: ident.Data == ELFData.BigEndian,
                    is64Bit: ident.Class == ELFClass.Class64);
                Reader headerReader = new(dumpAddressSpace, layoutManager);

                ELFHeader header = headerReader.Read<ELFHeader>(0);
                // Validate Signature
                Assert.True(header.IsIdentMagicValid.Check());
                // Validate ELF file is a core dump
                Assert.Equal(ELFHeaderType.Core, header.Type);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                if (runner.Environment.ContainsKey(EnableElfDumpOnMacOS))
                {
                    ELFHeader header = dumpReader.Read<ELFHeader>(0);
                    // Validate Signature
                    Assert.True(header.IsIdentMagicValid.Check());
                    // Validate ELF file is a core dump
                    Assert.Equal(ELFHeaderType.Core, header.Type);
                }
                else
                {
                    MachHeader header = dumpReader.Read<MachHeader>(0);
                    // Validate Signature
                    Assert.True(header.IsMagicValid.Check());
                    // Validate MachO file is a core dump
                    Assert.True(header.IsFileTypeValid.Check());
                    Assert.Equal(MachHeaderFileType.Core, header.FileType);
                }
            }
            else
            {
                throw new NotImplementedException("Dump header check not implemented for this OS platform.");
            }
        }
        */


    }
}
