﻿// Licensed to the .NET Foundation under one or more agreements.
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
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace CollectionRuleActions.UnitTests
{
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

        [Theory]
        [MemberData(nameof(ActionTestsHelper.GetTfmsAndDumpTypes), MemberType = typeof(ActionTestsHelper))]
        public Task CollectDumpAction_Success(TargetFrameworkMoniker tfm, DumpType dumpType)
        {
            return Retry(dumpType, () => CollectDumpAction_SuccessCore(tfm, dumpType));
        }

        private async Task CollectDumpAction_SuccessCore(TargetFrameworkMoniker tfm, DumpType dumpType)
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

                rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddCollectDumpAction(ActionTestsConstants.ExpectedEgressProvider, dumpType)
                    .SetStartupTrigger();
            }, async host =>
            {
                CollectDumpOptions options = ActionTestsHelper.GetActionOptions<CollectDumpOptions>(host, DefaultRuleName);

                ICollectionRuleActionFactoryProxy factory;
                Assert.True(host.Services.GetService<ICollectionRuleActionOperations>().TryCreateFactory(KnownCollectionRuleActions.CollectDump, out factory));

                EndpointInfoSourceCallback callback = new(_outputHelper);
                await using ServerSourceHolder sourceHolder = await _endpointUtilities.StartServerAsync(callback);

                await using AppRunner runner = _endpointUtilities.CreateAppRunner(Assembly.GetExecutingAssembly(), sourceHolder.TransportName, tfm);

                Task<IEndpointInfo> newEndpointInfoTask = callback.WaitAddedEndpointInfoAsync(runner, CommonTestTimeouts.StartProcess);

                await runner.ExecuteAsync(async () =>
                {
                    IEndpointInfo endpointInfo = await newEndpointInfoTask;

                    ICollectionRuleAction action = factory.Create(endpointInfo, options);

                    CollectionRuleActionResult result = await ActionTestsHelper.ExecuteAndDisposeAsync(action, CommonTestTimeouts.DumpTimeout);

                    string egressPath = ActionTestsHelper.ValidateEgressPath(result);

                    using FileStream dumpStream = new(egressPath, FileMode.Open, FileAccess.Read);
                    Assert.NotNull(dumpStream);

                    await DumpTestUtilities.ValidateDump(runner.Environment.ContainsKey(DumpTestUtilities.EnableElfDumpOnMacOS), dumpStream);

                    await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                });
            });
        }

        private async Task Retry(DumpType type, Func<Task> func, int attemptCount = 3)
        {
            bool isMacOSFullDump =
                RuntimeInformation.IsOSPlatform(OSPlatform.OSX) &&
                type == DumpType.Full;

            int attemptIteration = 0;
            while (true)
            {
                attemptIteration++;
                _outputHelper.WriteLine("===== Attempt #{0} =====", attemptIteration);
                try
                {
                    await func();

                    break;
                }
                catch (TaskCanceledException) when (attemptIteration < attemptCount && isMacOSFullDump)
                {
                    // Full dumps on MacOS sometimes take a very long time (longer than 100 seconds, the default
                    // HttpClient timeout). Retry the test when this condition is detected.
                }
            }
        }
    }
}
