// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using System.Threading;
using System;
using System.IO;
using System.Globalization;
using Xunit.Abstractions;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Fixtures;
using Microsoft.Extensions.Logging;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using System.Collections.Generic;
using static Microsoft.Diagnostics.Monitoring.Tool.UnitTests.EndpointInfoSourceTests;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Tools.Monitor;
using System.Reflection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.FileFormats;
using System.Runtime.InteropServices;
using Microsoft.FileFormats.ELF;
using Microsoft.FileFormats.MachO;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests;
using static Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.DumpTests;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    public sealed class CollectDumpActionTests
    {
        private static readonly TimeSpan GetEndpointInfoTimeout = TimeSpan.FromSeconds(10);
        //private const string FileProviderName = "files";

        private const int TokenTimeoutMs = 60000; // Arbitrarily set to 1 minute -> potentially needs to be bigger...?
                                                  //private const int DelayMs = 1000;

        private const string EnableElfDumpOnMacOS = "COMPlus_DbgEnableElfDumpOnMacOS";
        private const string DefaultRuleName = "Default";
        private const string TempEgressDirectory = "/tmp";

        private IServiceProvider _serviceProvider;
        private ILogger<CollectDumpAction> _logger;
        private ITestOutputHelper _outputHelper;

        public CollectDumpActionTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Fact]
        public async Task CollectDumpAction_FileEgressProvider()
        {
            const string ExpectedEgressProvider = "TmpEgressProvider";
            const DumpType ExpectedDumpType = DumpType.Mini;

            string uniqueEgressDirectory = TempEgressDirectory + Guid.NewGuid();

            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                rootOptions.CreateCollectionRule(DefaultRuleName)
                    .SetStartupTrigger()
                    .AddCollectDumpAction(ExpectedDumpType, ExpectedEgressProvider);

                rootOptions.AddFileSystemEgress(ExpectedEgressProvider, uniqueEgressDirectory);
            }, async host =>
            {
                _serviceProvider = host.Services;
                _logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger<CollectDumpAction>();

                CollectDumpAction action = new(_logger, _serviceProvider);

                CollectDumpOptions options = new();

                options.Egress = ExpectedEgressProvider;
                options.Type = ExpectedDumpType;

                using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TokenTimeoutMs);

                ServerEndpointInfoCallback callback = new(_outputHelper);
                await using var source = CreateServerSource(out string transportName, callback);
                source.Start();

                var endpointInfos = await GetEndpointInfoAsync(source);
                Assert.Empty(endpointInfos);

                AppRunner runner = CreateAppRunner(transportName, TargetFrameworkMoniker.Net60); // Arbitrarily chose Net60

                Task newEndpointInfoTask = callback.WaitForNewEndpointInfoAsync(runner, CommonTestTimeouts.StartProcess);

                await runner.ExecuteAsync(async () =>
                {
                    await newEndpointInfoTask;

                    endpointInfos = await GetEndpointInfoAsync(source);

                    var endpointInfo = Assert.Single(endpointInfos);
                    Assert.NotNull(endpointInfo.CommandLine);
                    Assert.NotNull(endpointInfo.OperatingSystem);
                    Assert.NotNull(endpointInfo.ProcessArchitecture);
                    VerifyConnection(runner, endpointInfo);

                    CollectionRuleActionResult result = await action.ExecuteAsync(options, endpointInfo, cancellationTokenSource.Token);

                    string egressPath = result.OutputValues["EgressPath"];

                    if (!File.Exists(egressPath))
                    {
                        throw new FileNotFoundException(string.Format(CultureInfo.InvariantCulture, Tools.Monitor.Strings.ErrorMessage_FileNotFound, egressPath));
                    }
                    else
                    {
                        using (StreamReader reader = new StreamReader(egressPath, true))
                        {
                            Stream dumpStream = reader.BaseStream;

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

                    }

                    await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                });

                await Task.Delay(TimeSpan.FromSeconds(1));

                endpointInfos = await GetEndpointInfoAsync(source);

                Assert.Empty(endpointInfos);

                try
                {
                    DirectoryInfo outputDirectory = Directory.CreateDirectory(uniqueEgressDirectory);

                    try
                    {
                        outputDirectory?.Delete(recursive: true);
                    }
                    catch
                    {
                    }
                }
                catch
                {
                }
            });
        }

        private ServerEndpointInfoSource CreateServerSource(out string transportName, ServerEndpointInfoCallback callback = null)
        {
            DiagnosticPortHelper.Generate(DiagnosticPortConnectionMode.Listen, out _, out transportName);
            _outputHelper.WriteLine("Starting server endpoint info source at '" + transportName + "'.");

            List<IEndpointInfoSourceCallbacks> callbacks = new();
            if (null != callback)
            {
                callbacks.Add(callback);
            }
            return new ServerEndpointInfoSource(transportName, callbacks);
        }

        private AppRunner CreateAppRunner(string transportName, TargetFrameworkMoniker tfm, int appId = 1)
        {
            AppRunner appRunner = new(_outputHelper, Assembly.GetExecutingAssembly(), appId, tfm);
            appRunner.ConnectionMode = DiagnosticPortConnectionMode.Connect;
            appRunner.DiagnosticPortPath = transportName;
            appRunner.ScenarioName = TestAppScenarios.AsyncWait.Name;
            return appRunner;
        }

        private async Task<IEnumerable<IEndpointInfo>> GetEndpointInfoAsync(ServerEndpointInfoSource source)
        {
            _outputHelper.WriteLine("Getting endpoint infos.");
            using CancellationTokenSource cancellationSource = new(GetEndpointInfoTimeout);
            return await source.GetEndpointInfoAsync(cancellationSource.Token);
        }

        /// <summary>
        /// Verifies basic information on the connection and that it matches the target process from the runner.
        /// </summary>
        private static void VerifyConnection(AppRunner runner, IEndpointInfo endpointInfo)
        {
            Assert.NotNull(runner);
            Assert.NotNull(endpointInfo);
            Assert.Equal(runner.ProcessId, endpointInfo.ProcessId);
            Assert.NotEqual(Guid.Empty, endpointInfo.RuntimeInstanceCookie);
            Assert.NotNull(endpointInfo.Endpoint);
        }
    }
}
