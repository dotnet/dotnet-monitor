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
using Microsoft.Extensions.Logging;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using System.Collections.Generic;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Tools.Monitor;
using System.Reflection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using static Microsoft.Diagnostics.Monitoring.Tool.UnitTests.EndpointUtilities; // Currently in PR

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    public sealed class CollectGCDumpActionTests
    {
        private const string TempEgressDirectory = "/tmp";
        private const string ExpectedEgressProvider = "TmpEgressProvider";
        private const string DefaultRuleName = "Default";

        private ITestOutputHelper _outputHelper;
        private readonly EndpointUtilities _endpointUtilities;

        public CollectGCDumpActionTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
            _endpointUtilities = new(_outputHelper);
        }

        [Fact]
        public async Task CollectGCDumpAction_Success()
        {
            try
            {
                string uniqueEgressDirectory = TempEgressDirectory + Guid.NewGuid();

                await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
                {
                    rootOptions.AddFileSystemEgress(ExpectedEgressProvider, uniqueEgressDirectory);

                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .AddCollectGCDumpAction(ExpectedEgressProvider)
                        .SetStartupTrigger();
                }, async host =>
                {
                    IOptionsMonitor<CollectionRuleOptions> ruleOptionsMonitor = host.Services.GetService<IOptionsMonitor<CollectionRuleOptions>>();
                    CollectGCDumpOptions options = (CollectGCDumpOptions)ruleOptionsMonitor.Get(DefaultRuleName).Actions[0].Settings;

                    ICollectionRuleActionProxy action;
                    Assert.True(host.Services.GetService<ICollectionRuleActionOperations>().TryCreateAction(KnownCollectionRuleActions.CollectGCDump, out action));

                    EndpointInfoSourceCallback callback = new(_outputHelper);
                    await using var source = _endpointUtilities.CreateServerSource(out string transportName, callback);
                    source.Start();

                    AppRunner runner = _endpointUtilities.CreateAppRunner(transportName, TargetFrameworkMoniker.Net60); // Arbitrarily chose Net60; should we test against other frameworks?

                    Task newEndpointInfoTask = callback.WaitForNewEndpointInfoAsync(runner, CommonTestTimeouts.StartProcess);

                    await runner.ExecuteAsync(async () =>
                    {
                        IEndpointInfo endpointInfo = await newEndpointInfoTask;

                        using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(CommonTestTimeouts.GCDumpTimeout);
                        CollectionRuleActionResult result = await action.ExecuteAsync(options, endpointInfo, cancellationTokenSource.Token);

                        // Currently not doing any validation on the validity of the GCDump (just checking that the file exists)
                        Assert.NotNull(result.OutputValues);
                        Assert.True(result.OutputValues.TryGetValue(CollectDumpAction.egressPath, out string egressPath));
                        Assert.True(File.Exists(egressPath));

                        await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                    });
                });
            }
            finally
            {
                try
                {
                    uniqueEgressDirectory?.Delete(recursive: true);
                }
                catch
                {
                }
            }
        }
    }
}