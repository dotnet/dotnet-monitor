﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Monitoring.WebApi;
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
    public sealed class CollectLogsActionTests
    {
        private const string ExpectedEgressProvider = "TmpEgressProvider";
        private const string DefaultRuleName = "Default";

        private ITestOutputHelper _outputHelper;
        private readonly EndpointUtilities _endpointUtilities;

        public CollectLogsActionTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
            _endpointUtilities = new(_outputHelper);
        }

        [Fact]
        public async Task CollectLogsAction_Success()
        {
            using TemporaryDirectory tempDirectory = new(_outputHelper);

            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                rootOptions.AddFileSystemEgress(ExpectedEgressProvider, tempDirectory.FullName);

                rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddCollectLogsAction(ExpectedEgressProvider, out CollectLogsOptions collectLogsOptions)
                    .SetStartupTrigger();

                collectLogsOptions.Duration = TimeSpan.FromSeconds(2);
            }, async host =>
            {
                IOptionsMonitor<CollectionRuleOptions> ruleOptionsMonitor = host.Services.GetService<IOptionsMonitor<CollectionRuleOptions>>();
                CollectLogsOptions options = (CollectLogsOptions)ruleOptionsMonitor.Get(DefaultRuleName).Actions[0].Settings;

                ICollectionRuleActionFactoryProxy factory;
                Assert.True(host.Services.GetService<ICollectionRuleActionOperations>().TryCreateFactory(KnownCollectionRuleActions.CollectLogs, out factory));

                EndpointInfoSourceCallback callback = new(_outputHelper);
                await using var source = _endpointUtilities.CreateServerSource(out string transportName, callback);
                source.Start();

                AppRunner runner = _endpointUtilities.CreateAppRunner(transportName, TargetFrameworkMoniker.Net60); // Arbitrarily chose Net60;

                Task<IEndpointInfo> newEndpointInfoTask = callback.WaitForNewEndpointInfoAsync(runner, CommonTestTimeouts.StartProcess);

                await runner.ExecuteAsync(async () =>
                {
                    IEndpointInfo endpointInfo = await newEndpointInfoTask;

                    ICollectionRuleAction action = factory.Create(endpointInfo, options);

                    using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(CommonTestTimeouts.LogsTimeout);

                    CollectionRuleActionResult result;
                    try
                    {
                        await action.StartAsync(cancellationTokenSource.Token);

                        result = await action.WaitForCompletionAsync(cancellationTokenSource.Token);
                    }
                    finally
                    {
                        await DisposableHelper.DisposeAsync(action);
                    }

                    // Not currently doing any validation on the Logs itself for validity; just checking that the file was created. Should this use the existing Logs tests?
                    Assert.NotNull(result.OutputValues);
                    Assert.True(result.OutputValues.TryGetValue(CollectionRuleActionConstants.EgressPathOutputValueName, out string egressPath));
                    Assert.True(File.Exists(egressPath));

                    await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                });
            });
        }
    }
}