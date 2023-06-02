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

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.FunctionalTests
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

        [Theory]
        [InlineData(TestAppScenarios.FunctionProbes.Commands.ProbeInstallation)]
        [InlineData(TestAppScenarios.FunctionProbes.Commands.ProbeUninstallation)]
        [InlineData(TestAppScenarios.FunctionProbes.Commands.CapturePrimitives)]
        public async Task TestScenario(string command)
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
                    runner.Architecture = System.Runtime.InteropServices.Architecture.X64;
                },
                configureTool: runner =>
                {
                    runner.ConfigurationFromEnvironment.EnableInProcessFeatures();
                    runner.EnableCallStacksFeature = true;
                });
        }
    }
}
