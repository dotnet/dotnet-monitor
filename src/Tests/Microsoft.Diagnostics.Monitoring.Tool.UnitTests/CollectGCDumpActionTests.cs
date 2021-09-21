// Licensed to the .NET Foundation under one or more agreements.
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
using System.Text;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    public sealed class CollectGCDumpActionTests
    {
        private const string ExpectedEgressProvider = "TmpEgressProvider";
        private const string DefaultRuleName = "GCDumpTestRule";

        readonly private ITestOutputHelper _outputHelper;
        private readonly EndpointUtilities _endpointUtilities;

        public CollectGCDumpActionTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
            _endpointUtilities = new(_outputHelper);
        }

        [Fact]
        public async Task CollectGCDumpAction_Success()
        {
            using TemporaryDirectory tempDirectory = new(_outputHelper);

            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                rootOptions.AddFileSystemEgress(ExpectedEgressProvider, tempDirectory.FullName);

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

                AppRunner runner = _endpointUtilities.CreateAppRunner(transportName, TargetFrameworkMoniker.Net60); // Arbitrarily chose Net60

                Task<IEndpointInfo> newEndpointInfoTask = callback.WaitForNewEndpointInfoAsync(runner, CommonTestTimeouts.StartProcess);

                await runner.ExecuteAsync(async () =>
                {
                    IEndpointInfo endpointInfo = await newEndpointInfoTask;

                    using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(CommonTestTimeouts.GCDumpTimeout);
                    CollectionRuleActionResult result = await action.ExecuteAsync(options, endpointInfo, cancellationTokenSource.Token);

                    Assert.NotNull(result.OutputValues);
                    Assert.True(result.OutputValues.TryGetValue(CollectionRuleActionConstants.EgressPathOutputValueName, out string egressPath));
                    Assert.True(File.Exists(egressPath));

                    using FileStream gcdumpStream = new(egressPath, FileMode.Open, FileAccess.Read);
                    Assert.NotNull(gcdumpStream);

                    await ValidateGCDump(gcdumpStream);

                    await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                });
            });
        }

        private static async Task ValidateGCDump(Stream gcdumpStream)
        {
            using CancellationTokenSource cancellation = new(CommonTestTimeouts.GCDumpTimeout);
            byte[] buffer = await gcdumpStream.ReadBytesAsync(24, cancellation.Token);

            const string knownHeaderText = "!FastSerialization.1";

            Encoding enc8 = Encoding.UTF8;

            string headerText = enc8.GetString(buffer, 4, knownHeaderText.Length);

            Assert.Equal(knownHeaderText, headerText);
        }
    }
}