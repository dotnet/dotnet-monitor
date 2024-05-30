// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Fixtures;
using Microsoft.Diagnostics.Tools.Monitor.StartupHook;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

#if !NET7_0_OR_GREATER
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Runners;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Logging;
#endif

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    [Collection(DefaultCollectionFixture.Name)]
    public class FunctionProbesTests
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITestOutputHelper _outputHelper;

        public FunctionProbesTests(ITestOutputHelper outputHelper, ServiceProviderFixture serviceProviderFixture)
        {
            _httpClientFactory = serviceProviderFixture.ServiceProvider.GetService<IHttpClientFactory>();
            _outputHelper = outputHelper;
        }

        /// <summary>
        /// Retrieves all available function probes test scenarios for all available profiler
        /// architectures.
        /// </summary>
        public static IEnumerable<object[]> GetAllTestScenarios()
        {
            List<object[]> arguments = new();
            IEnumerable<object[]> testArchitectures = ProfilerHelper.GetArchitecture();
            List<string> commands = typeof(TestAppScenarios.FunctionProbes.SubScenarios).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .Select(p => p.Name)
                .ToList();

            Assert.NotEmpty(commands);

            foreach (object[] archArgs in testArchitectures)
            {
                foreach (string command in commands)
                {
                    arguments.Add(archArgs.Concat(new object[] { command }).ToArray());
                }
            }

            return arguments;
        }

#if !NET7_0_OR_GREATER
        [Theory(Skip ="This test installs the mutating profiler which is not enabled on net6 or lower.")]
#else
        [Theory]
#endif
        [MemberData(nameof(FunctionProbesTests.GetAllTestScenarios), MemberType = typeof(FunctionProbesTests))]
        public async Task RunTestScenario(Architecture targetArchitecture, string subScenario)
        {
            await using AppRunner appRunner = new(_outputHelper, Assembly.GetExecutingAssembly())
            {
                Architecture = targetArchitecture,
                ScenarioName = TestAppScenarios.FunctionProbes.Name,
                SubScenarioName = subScenario
            };

            // Enable the mutating profiler
            string profilerPath = NativeLibraryHelper.GetSharedLibraryPath(targetArchitecture, ProfilerIdentifiers.MutatingProfiler.LibraryRootFileName);
            appRunner.Environment.Add(ProfilerHelper.ClrEnvVarEnableNotificationProfilers, ProfilerHelper.ClrEnvVarEnabledValue);
            appRunner.Environment.Add(ProfilerHelper.ClrEnvVarEnableProfiling, ProfilerHelper.ClrEnvVarEnabledValue);
            appRunner.Environment.Add(ProfilerHelper.ClrEnvVarProfiler, ProfilerIdentifiers.MutatingProfiler.Clsid.StringWithBraces);
            appRunner.Environment.Add(ProfilerHelper.ClrEnvVarProfilerPath, profilerPath);
            appRunner.Environment.Add(ProfilerIdentifiers.MutatingProfiler.EnvironmentVariables.ModulePath, profilerPath);

            // The profiler checks this env variable to enable parameter capturing features.
            appRunner.Environment.Add(InProcessFeaturesIdentifiers.EnvironmentVariables.ParameterCapturing.Enable, "1");

            await appRunner.ExecuteAsync(() => Task.CompletedTask);

            Assert.Equal(0, appRunner.ExitCode);
        }

#if !NET7_0_OR_GREATER
        [Theory]
        [MemberData(nameof(ProfilerHelper.GetArchitecture), MemberType = typeof(ProfilerHelper))]
        public async Task ValidateProfilerIsNotInstalledOnNet6(Architecture targetArchitecture)
        {
            await ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                DiagnosticPortConnectionMode.Listen,
                TestAppScenarios.FunctionProbes.Name,
                appValidate: (runner, client) => { return Task.CompletedTask; },
                configureApp: runner =>
                {
                    runner.Architecture = targetArchitecture;
                },
                configureTool: runner =>
                {
                    runner.ConfigurationFromEnvironment.EnableInProcessFeatures();
                    runner.ConfigurationFromEnvironment.InProcessFeatures.ParameterCapturing = new()
                    {
                        Enabled = true
                    };
                },
                profilerLogLevel: LogLevel.Trace,
                subScenarioName: TestAppScenarios.FunctionProbes.SubScenarios.ValidateNoMutatingProfiler);
        }
#endif // NET7_0_OR_GREATER
    }
}
