// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    public sealed class CollectTraceActionTests
    {
        private const string DefaultRuleName = "TraceTestRule";

        private ITestOutputHelper _outputHelper;
        private readonly EndpointUtilities _endpointUtilities;

        public CollectTraceActionTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
            _endpointUtilities = new(_outputHelper);
        }

        [Theory]
        [MemberData(nameof(ActionTestsHelper.GetTfmsAndTraceProfiles), MemberType = typeof(ActionTestsHelper))]
        public async Task CollectTraceAction_ProfileSuccess(TargetFrameworkMoniker tfm, TraceProfile traceProfile)
        {
            using TemporaryDirectory tempDirectory = new(_outputHelper);

            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                rootOptions.AddFileSystemEgress(ActionTestsConstants.ExpectedEgressProvider, tempDirectory.FullName);

                rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddCollectTraceAction(traceProfile, ActionTestsConstants.ExpectedEgressProvider, options =>
                    {
                        options.Duration = TimeSpan.FromSeconds(2);
                    })
                    .SetStartupTrigger();
            }, async host =>
            {
                await PerformTrace(host, tfm);
            });
        }

        [Theory]
        [MemberData(nameof(ActionTestsHelper.GetTfms), MemberType = typeof(ActionTestsHelper))]
        public async Task CollectTraceAction_ProvidersSuccess(TargetFrameworkMoniker tfm)
        {
            List<EventPipeProvider> ExpectedProviders = new()
            {
                new() { Name = "Microsoft-Extensions-Logging" }
            };

            using TemporaryDirectory tempDirectory = new(_outputHelper);

            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                rootOptions.AddFileSystemEgress(ActionTestsConstants.ExpectedEgressProvider, tempDirectory.FullName);

                rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddCollectTraceAction(ExpectedProviders, ActionTestsConstants.ExpectedEgressProvider, options =>
                    {
                        options.Duration = TimeSpan.FromSeconds(2);
                    })
                    .SetStartupTrigger();
            }, async host =>
            {
                await PerformTrace(host, tfm);
            });
        }

        private async Task PerformTrace(IHost host, TargetFrameworkMoniker tfm)
        {
            CollectTraceOptions options = ActionTestsHelper.GetActionOptions<CollectTraceOptions>(host, DefaultRuleName);

            ICollectionRuleActionFactoryProxy factory;
            Assert.True(host.Services.GetService<ICollectionRuleActionOperations>().TryCreateFactory(KnownCollectionRuleActions.CollectTrace, out factory));

            EndpointInfoSourceCallback callback = new(_outputHelper);
            await using ServerSourceHolder sourceHolder = await _endpointUtilities.StartServerAsync(callback);

            AppRunner runner = _endpointUtilities.CreateAppRunner(sourceHolder.TransportName, tfm);

            Task<IEndpointInfo> newEndpointInfoTask = callback.WaitAddedEndpointInfoAsync(runner, CommonTestTimeouts.StartProcess);

            await runner.ExecuteAsync(async () =>
            {
                IEndpointInfo endpointInfo = await newEndpointInfoTask;

                ICollectionRuleAction action = factory.Create(endpointInfo, options);

                CollectionRuleActionResult result = await ActionTestsHelper.ExecuteAndDisposeAsync(action, CommonTestTimeouts.TraceTimeout);

                string egressPath = ActionTestsHelper.ValidateEgressPath(result);

                using FileStream traceStream = new(egressPath, FileMode.Open, FileAccess.Read);
                Assert.NotNull(traceStream);

                await ValidateTrace(traceStream);

                await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
            });
        }

        private static async Task ValidateTrace(Stream traceStream)
        {
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
