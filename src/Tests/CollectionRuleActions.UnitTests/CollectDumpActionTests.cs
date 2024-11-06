// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace CollectionRuleActions.UnitTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    [Collection(TestCollections.CollectionRuleActions)]
    public sealed class CollectDumpActionTests
    {
        private const string DefaultRuleName = nameof(CollectDumpActionTests);

        private ITestOutputHelper _outputHelper;
        private readonly EndpointUtilities _endpointUtilities;

        public CollectDumpActionTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
            _endpointUtilities = new(_outputHelper);
        }

        [Theory(Skip = "Flaky")]
        [MemberData(nameof(ActionTestsHelper.GetTfmsAndDumpTypes), MemberType = typeof(ActionTestsHelper))]
        public Task CollectDumpAction_Success(TargetFrameworkMoniker tfm, DumpType dumpType)
        {
            return RetryUtilities.RetryAsync(
                func: () => CollectDumpAction_SuccessCore(tfm, dumpType),
                shouldRetry: (Exception ex) => ex is TaskCanceledException,
                outputHelper: _outputHelper);
        }

        [Fact]
        public Task CollectDumpAction_CustomArtifactName()
        {
            // Code path should be unchanged between TFM and dump type
            return RetryUtilities.RetryAsync(
                func: () => CollectDumpAction_SuccessCore(TargetFrameworkMoniker.Current, DumpType.Mini, artifactName: Guid.NewGuid().ToString("n")),
                shouldRetry: (Exception ex) => ex is TaskCanceledException,
                outputHelper: _outputHelper);
        }

        private async Task CollectDumpAction_SuccessCore(TargetFrameworkMoniker tfm, DumpType dumpType, string artifactName = null)
        {
            // MacOS dumps inconsistently segfault the runtime on .NET 5: https://github.com/dotnet/dotnet-monitor/issues/174
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && tfm == TargetFrameworkMoniker.Net50)
            {
                return;
            }

            using TemporaryDirectory tempDirectory = new(_outputHelper);

            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                rootOptions.AddFileSystemEgress(ActionTestsConstants.ExpectedEgressProvider, tempDirectory.FullName);

                CollectionRuleOptions options = rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddCollectDumpAction(ActionTestsConstants.ExpectedEgressProvider, o =>
                    {
                        o.ArtifactName = artifactName;
                        o.Type = dumpType;
                    })
                    .SetStartupTrigger();
            }, async host =>
            {
                CollectDumpOptions options = ActionTestsHelper.GetActionOptions<CollectDumpOptions>(host, DefaultRuleName);

                ICollectionRuleActionFactoryProxy factory;
                Assert.True(host.Services.GetService<ICollectionRuleActionOperations>().TryCreateFactory(KnownCollectionRuleActions.CollectDump, out factory));

                EndpointInfoSourceCallback callback = new(_outputHelper);
                await using ServerSourceHolder sourceHolder = await _endpointUtilities.StartServerAsync(callback);

                await using AppRunner runner = _endpointUtilities.CreateAppRunner(Assembly.GetExecutingAssembly(), sourceHolder.TransportName, tfm);

                Task<IProcessInfo> newProcessInfoTask = callback.WaitAddedProcessInfoAsync(runner, CommonTestTimeouts.StartProcess);

                await runner.ExecuteAsync(async () =>
                {
                    IProcessInfo processInfo = await newProcessInfoTask;

                    ICollectionRuleAction action = factory.Create(processInfo, options);

                    CollectionRuleActionResult result = await ActionTestsHelper.ExecuteAndDisposeAsync(action, CommonTestTimeouts.DumpTimeout);

                    string egressPath = ActionTestsHelper.ValidateEgressPath(result, artifactName);

                    using FileStream dumpStream = new(egressPath, FileMode.Open, FileAccess.Read);
                    Assert.NotNull(dumpStream);

                    await DumpTestUtilities.ValidateDump(runner.Environment.ContainsKey(DumpTestUtilities.EnableElfDumpOnMacOS), dumpStream);

                    await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                });
            });
        }
    }
}
