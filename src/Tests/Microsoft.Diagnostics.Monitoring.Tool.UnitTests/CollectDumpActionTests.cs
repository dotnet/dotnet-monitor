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

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    public sealed class CollectDumpActionTests
    {
        private const string ExpectedEgressProvider = "TmpEgressProvider";
        private const string DefaultRuleName = "DumpTestRule";

        private ITestOutputHelper _outputHelper;
        private readonly EndpointUtilities _endpointUtilities;

        public CollectDumpActionTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
            _endpointUtilities = new(_outputHelper);
        }

        [Theory]
        [InlineData(DumpType.Full)]
        [InlineData(DumpType.Mini)]
        [InlineData(DumpType.Triage)]
        [InlineData(DumpType.WithHeap)]
        [InlineData(null)]
        public async Task CollectDumpAction_Success(DumpType? dumpType)
        {
            using TemporaryDirectory tempDirectory = new(_outputHelper);

            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                rootOptions.AddFileSystemEgress(ExpectedEgressProvider, tempDirectory.FullName);

                rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddCollectDumpAction(ExpectedEgressProvider, dumpType)
                    .SetStartupTrigger();
            }, async host =>
            {
                ActionTestHelper<CollectDumpOptions> helper = new(host, _endpointUtilities, _outputHelper);

                await helper.TestAction(DefaultRuleName, KnownCollectionRuleActions.CollectDump, CommonTestTimeouts.DumpTimeout, (egressPath, runner) => DumpValidation(runner, egressPath));
            });
        }

        internal static async Task DumpValidation(AppRunner runner, string egressPath)
        {
            using FileStream dumpStream = new(egressPath, FileMode.Open, FileAccess.Read);
            Assert.NotNull(dumpStream);

            await DumpTestUtilities.ValidateDump(runner.Environment.ContainsKey(DumpTestUtilities.EnableElfDumpOnMacOS), dumpStream);

            await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
        }

    }
}