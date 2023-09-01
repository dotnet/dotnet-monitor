// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace CollectionRuleActions.UnitTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    [Collection(TestCollections.CollectionRuleActions)]
    public sealed class LoadProfilerActionTests
    {
        private const string DefaultRuleName = "ProfilerTestRule";

        private readonly ITestOutputHelper _outputHelper;
        private readonly EndpointUtilities _endpointUtilities;

        public LoadProfilerActionTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
            _endpointUtilities = new(_outputHelper);
        }

        /// <summary>
        /// Tests the LoadProfiler action using the monitor profiler.
        /// </summary>
        [Theory]
        [MemberData(nameof(ActionTestsHelper.GetTfmArchitectureProfilerPath), MemberType = typeof(ActionTestsHelper))]
        public async Task LoadProfilerAsStartupProfilerTest(TargetFrameworkMoniker tfm, Architecture architecture, string profilerPath)
        {
            string profilerFileName = Path.GetFileName(profilerPath);

            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddSetEnvironmentVariableAction(ProfilerIdentifiers.EnvironmentVariables.RuntimeInstanceId, ConfigurationTokenParser.RuntimeIdReference)
                    .AddLoadProfilerAction(options =>
                    {
                        options.Path = profilerPath;
                        options.Clsid = ProfilerIdentifiers.NotifyOnlyProfiler.Clsid.Guid;
                    })
                    .SetStartupTrigger();
            }, async host =>
            {
                LoadProfilerCallback callback = new(_outputHelper, host);
                await using ServerSourceHolder sourceHolder = await _endpointUtilities.StartServerAsync(callback);

                await using AppRunner runner = _endpointUtilities.CreateAppRunner(Assembly.GetExecutingAssembly(), sourceHolder.TransportName, tfm);
                runner.Architecture = architecture;
                runner.ScenarioName = TestAppScenarios.AsyncWait.Name;

                await runner.ExecuteAsync(async () =>
                {
                    // At this point, the profiler has already been initialized and managed code is already running.
                    // Use any of the initialization state of the profiler to validate that it is loaded.
                    await ProfilerHelper.VerifyProductVersionEnvironmentVariableAsync(runner, _outputHelper);

                    await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                });
            });
        }

        /// <summary>
        /// Set profiler as startup profiler before the runtime instance is resumed.
        /// </summary>
        private sealed class LoadProfilerCallback :
            EndpointInfoSourceCallback
        {
            private readonly IHost _host;

            public LoadProfilerCallback(ITestOutputHelper outputHelper, IHost host)
                : base(outputHelper)
            {
                _host = host ?? throw new ArgumentNullException(nameof(host));
            }

            public override async Task OnBeforeResumeAsync(IEndpointInfo endpointInfo, CancellationToken token)
            {
                SetEnvironmentVariableOptions envOptions = ActionTestsHelper.GetActionOptions<SetEnvironmentVariableOptions>(_host, DefaultRuleName, actionIndex: 0);
                Assert.True(_host.Services.GetService<ICollectionRuleActionOperations>().TryCreateFactory(KnownCollectionRuleActions.SetEnvironmentVariable, out ICollectionRuleActionFactoryProxy setEnvFactory));

                IProcessInfo processInfo = await ProcessInfoImpl.FromEndpointInfoAsync(endpointInfo, token);
                ICollectionRuleAction setEnvAction = setEnvFactory.Create(processInfo, envOptions);
                await ActionTestsHelper.ExecuteAndDisposeAsync(setEnvAction, CommonTestTimeouts.EnvVarsTimeout);

                // Load the profiler into the target process
                LoadProfilerOptions options = ActionTestsHelper.GetActionOptions<LoadProfilerOptions>(_host, DefaultRuleName, actionIndex: 1);

                ICollectionRuleActionFactoryProxy factory;
                Assert.True(_host.Services.GetService<ICollectionRuleActionOperations>().TryCreateFactory(KnownCollectionRuleActions.LoadProfiler, out factory));

                ICollectionRuleAction action = factory.Create(processInfo, options);

                CollectionRuleActionResult result = await ActionTestsHelper.ExecuteAndDisposeAsync(action, CommonTestTimeouts.LoadProfilerTimeout);
                Assert.Null(result.OutputValues);
            }
        }
    }
}
