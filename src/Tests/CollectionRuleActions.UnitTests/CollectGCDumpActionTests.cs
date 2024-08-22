// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Exceptions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace CollectionRuleActions.UnitTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    [Collection(TestCollections.CollectionRuleActions)]
    public sealed class CollectGCDumpActionTests
    {
        private const string DefaultRuleName = "GCDumpTestRule";

        private readonly ITestOutputHelper _outputHelper;
        private readonly EndpointUtilities _endpointUtilities;

        public CollectGCDumpActionTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
            _endpointUtilities = new(_outputHelper);
        }

        [Theory]
        [MemberData(nameof(ActionTestsHelper.GetTfms), MemberType = typeof(ActionTestsHelper))]
        public Task CollectGCDumpAction_Success(TargetFrameworkMoniker tfm)
        {
            return RetryUtilities.RetryAsync(
                func: () => CollectGCDumpAction_SuccessCore(tfm),
                // GC dumps can fail to be produced from the runtime because the pipeline doesn't get the expected
                // start, data, and stop events. The pipeline will throw an InvalidOperationException, which is
                // wrapped in a CollectionRuleActionException by the action.
                shouldRetry: (Exception ex) => (
                    ex is TaskCanceledException ||
                    (ex is CollectionRuleActionException && ex.InnerException is InvalidOperationException)),
                outputHelper: _outputHelper);
        }

        [Fact]
        public Task CollectGCDumpAction_CustomArtifactName()
        {
            return RetryUtilities.RetryAsync(
                func: () => CollectGCDumpAction_SuccessCore(TargetFrameworkMoniker.Current, artifactName: Guid.NewGuid().ToString("n")),
                shouldRetry: (Exception ex) => (
                    ex is TaskCanceledException ||
                    (ex is CollectionRuleActionException && ex.InnerException is InvalidOperationException)),
                outputHelper: _outputHelper);
        }

        private async Task CollectGCDumpAction_SuccessCore(TargetFrameworkMoniker tfm, string artifactName = null)
        {
            using TemporaryDirectory tempDirectory = new(_outputHelper);

            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                rootOptions.AddFileSystemEgress(ActionTestsConstants.ExpectedEgressProvider, tempDirectory.FullName);

                rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddCollectGCDumpAction(ActionTestsConstants.ExpectedEgressProvider, o => o.ArtifactName = artifactName)
                    .SetStartupTrigger();
            }, async host =>
            {
                CollectGCDumpOptions options = ActionTestsHelper.GetActionOptions<CollectGCDumpOptions>(host, DefaultRuleName);

                ICollectionRuleActionFactoryProxy factory;
                Assert.True(host.Services.GetService<ICollectionRuleActionOperations>().TryCreateFactory(KnownCollectionRuleActions.CollectGCDump, out factory));

                EndpointInfoSourceCallback callback = new(_outputHelper);
                await using ServerSourceHolder sourceHolder = await _endpointUtilities.StartServerAsync(callback);

                await using AppRunner runner = _endpointUtilities.CreateAppRunner(Assembly.GetExecutingAssembly(), sourceHolder.TransportName, tfm);

                Task<IProcessInfo> processInfoTask = callback.WaitAddedProcessInfoAsync(runner, CommonTestTimeouts.StartProcess);

                await runner.ExecuteAsync(async () =>
                {
                    IProcessInfo processInfo = await processInfoTask;

                    ICollectionRuleAction action = factory.Create(processInfo, options);

                    CollectionRuleActionResult result = await ActionTestsHelper.ExecuteAndDisposeAsync(action, CommonTestTimeouts.GCDumpTimeout);

                    string egressPath = ActionTestsHelper.ValidateEgressPath(result, artifactName);

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
