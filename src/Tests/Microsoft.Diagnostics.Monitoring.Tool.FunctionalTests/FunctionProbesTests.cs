// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Runners;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    [Collection(DefaultCollectionFixture.Name)]
    public class FunctionProbesTests
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITestOutputHelper _outputHelper;

        public static IEnumerable<object[]> GetTestScenarios()
        {
            List<object[]> arguments = new();

            IEnumerable<object[]> testArchitectures = ProfilerHelper.GetArchitecture();
            List<string> commands = typeof(TestAppScenarios.FunctionProbes.Commands).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
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

        public FunctionProbesTests(ITestOutputHelper outputHelper, ServiceProviderFixture serviceProviderFixture)
        {
            _httpClientFactory = serviceProviderFixture.ServiceProvider.GetService<IHttpClientFactory>();
            _outputHelper = outputHelper;
        }

        [Theory]
        [MemberData(nameof(FunctionProbesTests.GetTestScenarios), MemberType = typeof(FunctionProbesTests))]
        public async Task TestScenario(Architecture targetArchitecture, string command)
        {
            await ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                DiagnosticPortConnectionMode.Listen,
                TestAppScenarios.FunctionProbes.Name,
                appValidate: async (runner, client) =>
                {
                    await AppRunnerExtensions.SendCommandAsync(runner, command);
                },
                configureApp: runner =>
                {
                    runner.Architecture = targetArchitecture;
                },
                configureTool: runner =>
                {
                    runner.ConfigurationFromEnvironment.EnableInProcessFeatures();
                    runner.EnableCallStacksFeature = true;
                });
        }
    }
}
