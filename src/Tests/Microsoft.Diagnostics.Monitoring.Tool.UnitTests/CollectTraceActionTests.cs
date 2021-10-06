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
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using Microsoft.Diagnostics.Tracing;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    public sealed class CollectTraceActionTests
    {
        private const string ExpectedEgressProvider = "TmpEgressProvider";
        private const string DefaultRuleName = "TraceTestRule";

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
                ActionTestHelper<CollectTraceOptions> helper = new(host, _endpointUtilities, _outputHelper);

                await helper.TestAction(DefaultRuleName, KnownCollectionRuleActions.CollectTrace, CommonTestTimeouts.TraceTimeout, (egressPath, runner) => ValidateTrace(runner, egressPath));
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
                ActionTestHelper<CollectTraceOptions> helper = new(host, _endpointUtilities, _outputHelper);

                await helper.TestAction(DefaultRuleName, KnownCollectionRuleActions.CollectTrace, CommonTestTimeouts.TraceTimeout, (egressPath, runner) => ValidateTrace(runner, egressPath));
            });
        }

        private static async Task ValidateTrace(AppRunner runner, string egressPath)
        {
            using FileStream traceStream = new(egressPath, FileMode.Open, FileAccess.Read);
            Assert.NotNull(traceStream);

            using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            using var eventSource = new EventPipeEventSource(traceStream);

            // Dispose event source when cancelled.
            using var _ = cancellationTokenSource.Token.Register(() => eventSource.Dispose());

            bool foundTraceObject = false;

            eventSource.Dynamic.All += (TraceEvent obj) =>
            {
                foundTraceObject = true;
            };

            await Task.Run(() => Assert.True(eventSource.Process()), cancellationTokenSource.Token);

            Assert.True(foundTraceObject);
        }
    }
}