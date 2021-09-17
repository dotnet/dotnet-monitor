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
        private const string TempEgressDirectory = "/tmp";
        private const string ExpectedEgressProvider = "TmpEgressProvider";
        private const string DefaultRuleName = "Default";

        private ITestOutputHelper _outputHelper;
        private readonly EndpointUtilities _endpointUtilities;

        public CollectGCDumpActionTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
            _endpointUtilities = new(_outputHelper);
        }

        [Fact]
        public async Task CollectGCDumpAction_Success()
        {
            DirectoryInfo uniqueEgressDirectory = null;

            try
            {
                uniqueEgressDirectory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), TempEgressDirectory, Guid.NewGuid().ToString()));

                await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
                {
                    rootOptions.AddFileSystemEgress(ExpectedEgressProvider, uniqueEgressDirectory.FullName);

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

                    AppRunner runner = _endpointUtilities.CreateAppRunner(transportName, TargetFrameworkMoniker.Net60); // Arbitrarily chose Net60; should we test against other frameworks?

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
            finally
            {
                try
                {
                    uniqueEgressDirectory?.Delete(recursive: true);
                }
                catch
                {
                }
            }
        }

        private static async Task ValidateGCDump(Stream gcdumpStream)
        {
            byte[] buffer = new byte[24];

            const string knownHeaderText = "!FastSerialization.1";

            // Read enough to deserialize known header text.
            int read;
            int total = 0;
            using CancellationTokenSource cancellation = new(CommonTestTimeouts.GCDumpTimeout);
            while (total < buffer.Length && 0 != (read = await gcdumpStream.ReadAsync(buffer, total, buffer.Length - total, cancellation.Token)))
            {
                total += read;
            }

            byte[] subarray = new byte[knownHeaderText.Length];
            Array.Copy(buffer, 4, subarray, 0, subarray.Length); // The first header text character begins at the 4th index

            string headerText = Encoding.ASCII.GetString(subarray);

            Assert.Equal(knownHeaderText, headerText);
        }
    }
}