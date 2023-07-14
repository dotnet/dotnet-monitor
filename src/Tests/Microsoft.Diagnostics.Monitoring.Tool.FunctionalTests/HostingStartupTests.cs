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
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    [Collection(DefaultCollectionFixture.Name)]
    public class HostingStartupTests
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITestOutputHelper _outputHelper;

        public HostingStartupTests(ITestOutputHelper outputHelper, ServiceProviderFixture serviceProviderFixture)
        {
            _httpClientFactory = serviceProviderFixture.ServiceProvider.GetService<IHttpClientFactory>();
            _outputHelper = outputHelper;
        }


        [Theory]
        [InlineData(TestAppScenarios.HostingStartup.SubScenarios.VerifyAspNetApp, true)]
        [InlineData(TestAppScenarios.HostingStartup.SubScenarios.VerifyAspNetAppWithoutHostingStartup, false)]
        [InlineData(TestAppScenarios.HostingStartup.SubScenarios.VerifyNonAspNetAppNotImpacted, true)]

        public async Task HostingStartupLoadTests(string subScenario, bool tryLoadHostingStartup)
        {
            await ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                DiagnosticPortConnectionMode.Listen,
                TestAppScenarios.HostingStartup.Name,
                appValidate: (runner, client) => { return Task.CompletedTask; },
                configureApp: runner =>
                {
                    runner.EnableMonitorStartupHook = true;
                },
                configureTool: runner =>
                {
                    runner.ConfigurationFromEnvironment.EnableInProcessFeatures();
                    // Enable a feature that requires the hosting startup assembly.
                    runner.ConfigurationFromEnvironment.InProcessFeatures.ParameterCapturing.Enabled = tryLoadHostingStartup;
                },
                profilerLogLevel: LogLevel.Trace,
                subScenarioName: subScenario);
        }
    }
}
