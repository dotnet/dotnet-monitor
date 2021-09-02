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
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using System.Collections.Generic;
using static Microsoft.Diagnostics.Monitoring.Tool.UnitTests.EndpointInfoSourceTests;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Tools.Monitor;
using System.Reflection;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    public sealed class CollectDumpActionTests
    {
        private const string DefaultRuleName = "Default";
        private const string TempEgressDirectory = "/tmp";

        private IServiceProvider _serviceProvider;
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
        public async Task CollectDumpAction_FileEgressProvider(DumpType dumpType)
        {
            const string ExpectedEgressProvider = "TmpEgressProvider";
            DumpType ExpectedDumpType = dumpType;

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

                CollectDumpAction action = new(_serviceProvider);

                CollectDumpOptions options = new();

                options.Egress = ExpectedEgressProvider;
                options.Type = ExpectedDumpType;

                using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TestTimeouts.DumpTimeout);

                EndpointInfoSourceTests endpointInfoSourceTests = new(_outputHelper);

                ServerEndpointInfoCallback callback = new(_outputHelper);
                await using var source = endpointInfoSourceTests.CreateServerSource(out string transportName, callback);
                source.Start();

                var endpointInfos = await endpointInfoSourceTests.GetEndpointInfoAsync(source);
                Assert.Empty(endpointInfos);

                AppRunner runner = endpointInfoSourceTests.CreateAppRunner(transportName, TargetFrameworkMoniker.Net60); // Arbitrarily chose Net60; should we test against multiple versions?

                Task newEndpointInfoTask = callback.WaitForNewEndpointInfoAsync(runner, CommonTestTimeouts.StartProcess);

                await runner.ExecuteAsync(async () =>
                {
                    await newEndpointInfoTask;

                    endpointInfos = await endpointInfoSourceTests.GetEndpointInfoAsync(source);

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
