// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.Options;
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
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    public class CollectLiveMetricsActionTests
    {
        private readonly ITestOutputHelper _outputHelper;
        private readonly EndpointUtilities _endpointUtilities;

        private const string DefaultRuleName = "LiveMetricsTestRule";

        public CollectLiveMetricsActionTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
            _endpointUtilities = new(_outputHelper);
        }

        [Theory]
        [MemberData(nameof(ActionTestsHelper.GetTfms), MemberType = typeof(ActionTestsHelper))]
        private async Task CollectLiveMetricsAction_Success(TargetFrameworkMoniker tfm)
        {
            using TemporaryDirectory tempDirectory = new(_outputHelper);

            int durationSeconds = (int)(GlobalCounterOptionsDefaults.IntervalSeconds + 1);

            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                rootOptions.AddFileSystemEgress(ActionTestsConstants.ExpectedEgressProvider, tempDirectory.FullName);

                rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddCollectLiveMetricsAction(ActionTestsConstants.ExpectedEgressProvider, options =>
                    {
                        options.Duration = TimeSpan.FromSeconds(durationSeconds);
                    })
                    .SetStartupTrigger();
            }, async host =>
            {
                await CollectLiveMetrics(host, tfm);
            });
        }

        private async Task CollectLiveMetrics(IHost host, TargetFrameworkMoniker tfm)
        {
            CollectLiveMetricsOptions options = ActionTestsHelper.GetActionOptions<CollectLiveMetricsOptions>(host, DefaultRuleName);

            ICollectionRuleActionFactoryProxy factory;
            Assert.True(host.Services.GetService<ICollectionRuleActionOperations>().TryCreateFactory(KnownCollectionRuleActions.CollectLiveMetrics, out factory));

            EndpointInfoSourceCallback callback = new(_outputHelper);
            await using ServerSourceHolder sourceHolder = await _endpointUtilities.StartServerAsync(callback);

            AppRunner runner = _endpointUtilities.CreateAppRunner(sourceHolder.TransportName, tfm);

            Task<IEndpointInfo> newEndpointInfoTask = callback.WaitAddedEndpointInfoAsync(runner, CommonTestTimeouts.StartProcess);

            await runner.ExecuteAsync(async () =>
            {
                IEndpointInfo endpointInfo = await newEndpointInfoTask;

                ICollectionRuleAction action = factory.Create(endpointInfo, options);

                CollectionRuleActionResult result = await ActionTestsHelper.ExecuteAndDisposeAsync(action, CommonTestTimeouts.LiveMetricsTimeout);

                string egressPath = ActionTestsHelper.ValidateEgressPath(result);

                using FileStream liveMetricsStream = new(egressPath, FileMode.Open, FileAccess.Read);
                Assert.NotNull(liveMetricsStream);

                //await ValidateLiveMetrics(liveMetricsStream);

                StreamReader reader = new StreamReader(liveMetricsStream);

                string line = "";
                while ((line = reader.ReadLine()) != null)
                {
                    _outputHelper.WriteLine(line);
                }

                await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
            });
        }


        /*
        private static async Task ValidateLiveMetrics(Stream liveMetricsStream)
        {

        }*/


    }
}
