// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    public sealed class LoadProfilerActionTests
    {
        private const string DefaultRuleName = "ProfilerTestRule";

        // This environment variable name is embedded into the profiler and set at profiler initialization.
        // The value is determined BEFORE native build by the generation of the product version into the
        // _productversion.h header file.
        private const string ProductVersionEnvVarName = "DotnetMonitorProfiler_ProductVersion";
        private const string MonitorProfilerInstanceIdEnvVarName = "DotnetMonitorProfiler_RuntimeId";

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
            if (Architecture.X86 == architecture)
            {
                _outputHelper.WriteLine("Skipping x86 architecture since x86 host is not used at this time.");
                return;
            }

            string profilerFileName = Path.GetFileName(profilerPath);

            await TestHostHelper.CreateCollectionRulesHost(_outputHelper, rootOptions =>
            {
                rootOptions.CreateCollectionRule(DefaultRuleName)
                    .AddSetEnvironmentVariableAction(MonitorProfilerInstanceIdEnvVarName, ConfigurationTokenParser.RuntimeIdReference)
                    .AddLoadProfilerAction(options =>
                    {
                        options.Path = profilerPath;
                        options.Clsid = NativeLibraryHelper.MonitorProfilerClsid;
                    })
                    .SetStartupTrigger();
            }, async host =>
            {
                LoadProfilerCallback callback = new(_outputHelper, host);
                await using ServerSourceHolder sourceHolder = await _endpointUtilities.StartServerAsync(callback);

                AppRunner runner = _endpointUtilities.CreateAppRunner(sourceHolder.TransportName, tfm);
                runner.ScenarioName = TestAppScenarios.AsyncWait.Name;

                await runner.ExecuteAsync(async () =>
                {
                    // At this point, the profiler has already been initialized and managed code is already running.
                    // Use any of the initialization state of the profiler to validate that it is loaded.
                    string productVersion = await runner.GetEnvironmentVariable(ProductVersionEnvVarName, CommonTestTimeouts.EnvVarsTimeout);
                    Assert.False(string.IsNullOrEmpty(productVersion), "Expected product version to not be null or empty.");
                    _outputHelper.WriteLine("{0} = {1}", ProductVersionEnvVarName, productVersion);

                    await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                });
            });
        }

        /// <summary>
        /// Set profiler as startup profiler before the runtime instance is resumed.
        /// </summary>
        private class LoadProfilerCallback :
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
                ICollectionRuleAction setEnvAction = setEnvFactory.Create(endpointInfo, envOptions);
                await ActionTestsHelper.ExecuteAndDisposeAsync(setEnvAction, CommonTestTimeouts.EnvVarsTimeout);

                // Load the profiler into the target process
                LoadProfilerOptions options = ActionTestsHelper.GetActionOptions<LoadProfilerOptions>(_host, DefaultRuleName, actionIndex: 1);

                ICollectionRuleActionFactoryProxy factory;
                Assert.True(_host.Services.GetService<ICollectionRuleActionOperations>().TryCreateFactory(KnownCollectionRuleActions.LoadProfiler, out factory));

                ICollectionRuleAction action = factory.Create(endpointInfo, options);

                CollectionRuleActionResult result = await ActionTestsHelper.ExecuteAndDisposeAsync(action, CommonTestTimeouts.LoadProfilerTimeout);
                Assert.Null(result.OutputValues);
            }
        }
    }
}
