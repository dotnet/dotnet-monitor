// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace CollectionRuleActions.UnitTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    [Collection(TestCollections.CollectionRuleActions)]
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
            List<EventPipeProvider> ExpectedProviders =
            [
                new() { Name = "Microsoft-Extensions-Logging" }
            ];

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
            }, host => PerformTrace(host, tfm));
        }

        [Fact]
        public async Task CollectTraceAction_CustomArtifactName()
        {
            string artifactName = Guid.NewGuid().ToString("n");
            using TemporaryDirectory tempDirectory = new(_outputHelper);

            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                rootOptions.AddFileSystemEgress(ActionTestsConstants.ExpectedEgressProvider, tempDirectory.FullName);

                rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddCollectTraceAction(TraceProfile.Logs, ActionTestsConstants.ExpectedEgressProvider, options =>
                    {
                        options.ArtifactName = artifactName;
                        options.Duration = TimeSpan.FromSeconds(2);
                    })
                    .SetStartupTrigger();
            }, host => PerformTrace(host, TargetFrameworkMoniker.Current, artifactName));
        }

        private async Task PerformTrace(IHost host, TargetFrameworkMoniker tfm, string artifactName = null)
        {
            CollectTraceOptions options = ActionTestsHelper.GetActionOptions<CollectTraceOptions>(host, DefaultRuleName);

            ICollectionRuleActionFactoryProxy factory;
            Assert.True(host.Services.GetService<ICollectionRuleActionOperations>().TryCreateFactory(KnownCollectionRuleActions.CollectTrace, out factory));

            EndpointInfoSourceCallback callback = new(_outputHelper);
            await using ServerSourceHolder sourceHolder = await _endpointUtilities.StartServerAsync(callback);

            await using AppRunner runner = _endpointUtilities.CreateAppRunner(Assembly.GetExecutingAssembly(), sourceHolder.TransportName, tfm);

            Task<IProcessInfo> processInfoTask = callback.WaitAddedProcessInfoAsync(runner, CommonTestTimeouts.StartProcess);

            await runner.ExecuteAsync(async () =>
            {
                IProcessInfo processInfo = await processInfoTask;

                ICollectionRuleAction action = factory.Create(processInfo, options);

                CollectionRuleActionResult result = await ActionTestsHelper.ExecuteAndDisposeAsync(action, CommonTestTimeouts.TraceTimeout);

                string egressPath = ActionTestsHelper.ValidateEgressPath(result, artifactName);

                using FileStream traceStream = new(egressPath, FileMode.Open, FileAccess.Read);
                Assert.NotNull(traceStream);

                await TraceTestUtilities.ValidateTrace(traceStream);

                await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
            });
        }
    }
}
