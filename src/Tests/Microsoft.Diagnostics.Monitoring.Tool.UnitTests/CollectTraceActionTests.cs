﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    public sealed class CollectTraceActionTests
    {
        private const string TempEgressDirectory = "/tmp";
        private const string ExpectedEgressProvider = "TmpEgressProvider";
        private const string DefaultRuleName = "Default";

        private ITestOutputHelper _outputHelper;
        private readonly EndpointUtilities _endpointUtilities;

        public CollectTraceActionTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
            _endpointUtilities = new(_outputHelper);
        }

        [Theory]
        [InlineData(TraceProfile.Cpu)]
        [InlineData(TraceProfile.Http)]
        [InlineData(TraceProfile.Logs)]
        [InlineData(TraceProfile.Metrics)]
        public async Task CollectTraceAction_ProfileSuccess(TraceProfile traceProfile)
        {
            DirectoryInfo uniqueEgressDirectory = null;

            try
            {
                uniqueEgressDirectory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), TempEgressDirectory, Guid.NewGuid().ToString()));

                await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
                {
                    rootOptions.AddFileSystemEgress(ExpectedEgressProvider, uniqueEgressDirectory.FullName);

                    rootOptions.CreateCollectionRule(DefaultRuleName)
                        .AddCollectTraceAction(traceProfile, ExpectedEgressProvider)
                        .SetStartupTrigger();
                }, async host =>
                {
                    IOptionsMonitor<CollectionRuleOptions> ruleOptionsMonitor = host.Services.GetService<IOptionsMonitor<CollectionRuleOptions>>();
                    CollectTraceOptions options = (CollectTraceOptions)ruleOptionsMonitor.Get(DefaultRuleName).Actions[0].Settings;

                    ICollectionRuleActionProxy action;
                    Assert.True(host.Services.GetService<ICollectionRuleActionOperations>().TryCreateAction(KnownCollectionRuleActions.CollectTrace, out action));

                    EndpointInfoSourceCallback callback = new(_outputHelper);
                    await using var source = _endpointUtilities.CreateServerSource(out string transportName, callback);
                    source.Start();

                    AppRunner runner = _endpointUtilities.CreateAppRunner(transportName, TargetFrameworkMoniker.Net60); // Arbitrarily chose Net60; should we test against other frameworks?

                    Task<IEndpointInfo> newEndpointInfoTask = callback.WaitForNewEndpointInfoAsync(runner, CommonTestTimeouts.StartProcess);

                    await runner.ExecuteAsync(async () =>
                    {
                        IEndpointInfo endpointInfo = await newEndpointInfoTask;

                        using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(CommonTestTimeouts.TraceTimeout);
                        CollectionRuleActionResult result = await action.ExecuteAsync(options, endpointInfo, cancellationTokenSource.Token);

                        // Not currently doing any validation on the Trace itself for validity; just checking that the file was created.
                        Assert.NotNull(result.OutputValues);
                        Assert.True(result.OutputValues.TryGetValue(CollectDumpAction.EgressPathOutputValueName, out string egressPath));
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
