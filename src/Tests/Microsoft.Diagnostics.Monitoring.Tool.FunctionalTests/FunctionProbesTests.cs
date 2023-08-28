// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Fixtures;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Runners;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

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

        [Theory]
        [MemberData(nameof(FunctionProbesTests.GetAllTestScenarios), MemberType = typeof(FunctionProbesTests))]
        public async Task RunTestScenario(Architecture targetArchitecture, string subScenario)
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
                subScenarioName: subScenario);
        }
    }
}
