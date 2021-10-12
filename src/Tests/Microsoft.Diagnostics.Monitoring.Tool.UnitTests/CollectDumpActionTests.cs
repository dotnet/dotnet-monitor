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
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    public sealed class CollectDumpActionTests
    {
        private const string DefaultRuleName = "DumpTestRule";

        private ITestOutputHelper _outputHelper;
        private readonly EndpointUtilities _endpointUtilities;

        public CollectDumpActionTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
            _endpointUtilities = new(_outputHelper);
        }

        [Theory]
        [MemberData(nameof(ActionTestsHelper.GetTfmsAndDumpTypes), MemberType = typeof(ActionTestsHelper))]
        public async Task CollectDumpAction_Success(TargetFrameworkMoniker tfm, DumpType? dumpType)
        {
            using TemporaryDirectory tempDirectory = new(_outputHelper);

            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                rootOptions.AddFileSystemEgress(ActionTestsConstants.ExpectedEgressProvider, tempDirectory.FullName);

                rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddCollectDumpAction(ActionTestsConstants.ExpectedEgressProvider, dumpType)
                    .SetStartupTrigger();
            }, async host =>
            {
                CollectDumpOptions options = ActionTestsHelper.GetOptions<CollectDumpOptions>(DefaultRuleName, host);

                ICollectionRuleActionFactoryProxy factory;
                Assert.True(host.Services.GetService<ICollectionRuleActionOperations>().TryCreateFactory(KnownCollectionRuleActions.CollectDump, out factory));

                EndpointInfoSourceCallback callback = new(_outputHelper);
                await using ServerSourceHolder sourceHolder = await _endpointUtilities.StartServerAsync(callback);

                AppRunner runner = _endpointUtilities.CreateAppRunner(sourceHolder.TransportName, tfm);

                Task<IEndpointInfo> newEndpointInfoTask = callback.WaitAddedEndpointInfoAsync(runner, CommonTestTimeouts.StartProcess);

                await runner.ExecuteAsync(async () =>
                {
                    IEndpointInfo endpointInfo = await newEndpointInfoTask;

                    ICollectionRuleAction action = factory.Create(endpointInfo, options);

                    CollectionRuleActionResult result = await ActionTestsHelper.PerformAction(action, CommonTestTimeouts.DumpTimeout);

                    string egressPath = ActionTestsHelper.ValidateEgressPath(result);

                    using FileStream dumpStream = new(egressPath, FileMode.Open, FileAccess.Read);
                    Assert.NotNull(dumpStream);

                    await DumpTestUtilities.ValidateDump(runner.Environment.ContainsKey(DumpTestUtilities.EnableElfDumpOnMacOS), dumpStream);

                    await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                });
            });
        }
    }
}
