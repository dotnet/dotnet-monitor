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
using Microsoft.Diagnostics.Monitoring.TestCommon;
using static Microsoft.Diagnostics.Monitoring.Tool.UnitTests.EndpointInfoSourceTests;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    public sealed class CollectDumpActionTests
    {
        //private const string DefaultRuleName = "Default";
        private const string TempEgressDirectory = "/tmp";
        private const string ExpectedEgressProvider = "TmpEgressProvider";

        private ITestOutputHelper _outputHelper;

        public CollectDumpActionTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Theory]
        [InlineData(DumpType.Full)]
        [InlineData(DumpType.Mini)]
        [InlineData(DumpType.Triage)]
        [InlineData(DumpType.WithHeap)]
        [InlineData(null)]
        public async Task CollectDumpAction_Success(DumpType? dumpType)
        {
            DumpType ExpectedDumpType = (dumpType != null) ? dumpType.Value : CollectDumpOptionsDefaults.Type;

            string uniqueEgressDirectory = TempEgressDirectory + Guid.NewGuid();

            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                rootOptions.AddFileSystemEgress(ExpectedEgressProvider, uniqueEgressDirectory);
            }, async host =>
            {
                CollectDumpAction action = new(host.Services);

                CollectDumpOptions options = new();

                options.Egress = ExpectedEgressProvider;

                // This is for the scenario where no DumpType is specified
                if (dumpType != null)
                {
                    options.Type = ExpectedDumpType;
                }

                EndpointInfoSourceTests endpointInfoSourceTests = new(_outputHelper);

                ServerEndpointInfoCallback callback = new(_outputHelper);
                await using var source = endpointInfoSourceTests.CreateServerSource(out string transportName, callback);
                source.Start();

                var endpointInfos = await endpointInfoSourceTests.GetEndpointInfoAsync(source);
                Assert.Empty(endpointInfos);

                AppRunner runner = endpointInfoSourceTests.CreateAppRunner(transportName, TargetFrameworkMoniker.Net60); // Arbitrarily chose Net60; should we test against other frameworks?

                using CancellationTokenSource callbackCancellation = new(CommonTestTimeouts.StartProcess);
                Task newEndpointInfoTask = callback.WaitForNewEndpointInfoAsync(runner, callbackCancellation.Token);

                await runner.ExecuteAsync(async () =>
                {
                    await newEndpointInfoTask;

                    endpointInfos = await endpointInfoSourceTests.GetEndpointInfoAsync(source);

                    var endpointInfo = Assert.Single(endpointInfos);
                    Assert.NotNull(endpointInfo.CommandLine);
                    Assert.NotNull(endpointInfo.OperatingSystem);
                    Assert.NotNull(endpointInfo.ProcessArchitecture);
                    await VerifyConnectionAsync(runner, endpointInfo);

                    using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TestTimeouts.DumpTimeout);
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

                            await IDumpTestInterface.ValidateDump(runner, dumpStream);
                        }
                    }

                    await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                });

                await Task.Delay(TimeSpan.FromSeconds(1));

                endpointInfos = await endpointInfoSourceTests.GetEndpointInfoAsync(source);

                Assert.Empty(endpointInfos);

                try
                {
                    DirectoryInfo outputDirectory = Directory.CreateDirectory(uniqueEgressDirectory); // Do we have a better way of getting the current directory (to delete it)

                    outputDirectory?.Delete(recursive: true);
                }
                catch
                {
                }
            });
        }
    }
}
