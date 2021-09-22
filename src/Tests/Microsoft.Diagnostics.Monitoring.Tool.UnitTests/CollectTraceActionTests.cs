// Licensed to the .NET Foundation under one or more agreements.
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
using System.Text;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    public sealed class CollectTraceActionTests
    {
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
            using TemporaryDirectory tempDirectory = new(_outputHelper);

            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                rootOptions.AddFileSystemEgress(ExpectedEgressProvider, tempDirectory.FullName);

                rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddCollectTraceAction(traceProfile, ExpectedEgressProvider, out CollectTraceOptions collectTraceOptions)
                    .SetStartupTrigger();

                collectTraceOptions.Duration = TimeSpan.FromSeconds(2);
            }, async host =>
            {
                await PerformTrace(host);
            });
        }

        [Fact]
        public async Task CollectTraceAction_ProvidersSuccess()
        {
            List<EventPipeProvider> ExpectedProviders = new()
            {
                new() { Name = "Microsoft-Extensions-Logging" }
            };

            using TemporaryDirectory tempDirectory = new(_outputHelper);

            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                rootOptions.AddFileSystemEgress(ExpectedEgressProvider, tempDirectory.FullName);

                rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddCollectTraceAction(ExpectedProviders, ExpectedEgressProvider, out CollectTraceOptions collectTraceOptions)
                    .SetStartupTrigger();

                collectTraceOptions.Duration = TimeSpan.FromSeconds(2);
            }, async host =>
            {
                await PerformTrace(host);
            });
        }

        private async Task PerformTrace(IHost host)
        {
            IOptionsMonitor<CollectionRuleOptions> ruleOptionsMonitor = host.Services.GetService<IOptionsMonitor<CollectionRuleOptions>>();
            CollectTraceOptions options = (CollectTraceOptions)ruleOptionsMonitor.Get(DefaultRuleName).Actions[0].Settings;

            ICollectionRuleActionProxy action;
            Assert.True(host.Services.GetService<ICollectionRuleActionOperations>().TryCreateAction(KnownCollectionRuleActions.CollectTrace, out action));

            EndpointInfoSourceCallback callback = new(_outputHelper);
            await using var source = _endpointUtilities.CreateServerSource(out string transportName, callback);
            source.Start();

            AppRunner runner = _endpointUtilities.CreateAppRunner(transportName, TargetFrameworkMoniker.Net60); // Arbitrarily chose Net60

            Task<IEndpointInfo> newEndpointInfoTask = callback.WaitForNewEndpointInfoAsync(runner, CommonTestTimeouts.StartProcess);

            await runner.ExecuteAsync(async () =>
            {
                IEndpointInfo endpointInfo = await newEndpointInfoTask;

                using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(CommonTestTimeouts.TraceTimeout);
                CollectionRuleActionResult result = await action.ExecuteAsync(options, endpointInfo, cancellationTokenSource.Token);

                Assert.NotNull(result.OutputValues);
                Assert.True(result.OutputValues.TryGetValue(CollectionRuleActionConstants.EgressPathOutputValueName, out string egressPath));
                Assert.True(File.Exists(egressPath));

                using FileStream traceStream = new(egressPath, FileMode.Open, FileAccess.Read);
                Assert.NotNull(traceStream);

                await ValidateTrace(traceStream);

                await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
            });
        }

        // THIS NEEDS TO BE UPDATED TO TAKE ADVANTAGE OF NEW UTILITY FOR DESERIALIZING
        private static async Task ValidateTrace(Stream traceStream)
        {
            using CancellationTokenSource cancellation = new(CommonTestTimeouts.TraceTimeout);
            byte[] buffer = await traceStream.ReadBytesAsync(32, cancellation.Token);

            const string firstKnownHeaderText = "Nettrace";
            const string secondKnownHeaderText = "!FastSerialization.1";

            Encoding enc8 = Encoding.UTF8;

            byte[] firstSubarray = new byte[firstKnownHeaderText.Length];
            string firstHeaderText = enc8.GetString(buffer, 0, firstSubarray.Length);
            Assert.Equal(firstKnownHeaderText, firstHeaderText);

            byte[] secondSubarray = new byte[secondKnownHeaderText.Length];
            string secondHeaderText = enc8.GetString(buffer, 12, secondSubarray.Length);
            Assert.Equal(secondKnownHeaderText, secondHeaderText);
        }
    }
}