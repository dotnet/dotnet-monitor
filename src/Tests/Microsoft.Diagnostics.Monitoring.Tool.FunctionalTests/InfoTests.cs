﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Fixtures;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.HttpApi;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Runners;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    [Collection(DefaultCollectionFixture.Name)]
    public class InfoTests
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITestOutputHelper _outputHelper;

        public InfoTests(ITestOutputHelper outputHelper, ServiceProviderFixture serviceProviderFixture)
        {
            _httpClientFactory = serviceProviderFixture.ServiceProvider.GetService<IHttpClientFactory>();
            _outputHelper = outputHelper;
        }

        /// <summary>
        /// Tests that the info endpoint provides the expected output.
        /// </summary>
        [Theory]
        [InlineData(DiagnosticPortConnectionMode.Connect, false)]
        [InlineData(DiagnosticPortConnectionMode.Listen, true)]
        public Task InfoEndpointValidationTest(DiagnosticPortConnectionMode mode, bool enableInProcessFeatures)
        {
            return ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                mode,
                TestAppScenarios.AsyncWait.Name,
                appValidate: async (runner, client) =>
                {
                    // GET /info
                    DotnetMonitorInfo info = await client.GetInfoAsync();

                    Assert.NotNull(info.Version); // Not sure of how to get Dotnet Monitor version from within tests...
                    Assert.True(Version.TryParse(info.RuntimeVersion, out Version runtimeVersion), "Unable to parse version from RuntimeVersion property.");

                    Version currentAspNetVersion = TargetFrameworkMoniker.Net100.GetAspNetCoreFrameworkVersion();
                    Assert.Equal(currentAspNetVersion.Major, runtimeVersion.Major);
                    Assert.Equal(currentAspNetVersion.Minor, runtimeVersion.Minor);
                    Assert.Equal(currentAspNetVersion.Revision, runtimeVersion.Revision);

                    Assert.Equal(mode, info.DiagnosticPortMode);

                    if (mode == DiagnosticPortConnectionMode.Connect)
                    {
                        Assert.Null(info.DiagnosticPortName);
                    }
                    else if (mode == DiagnosticPortConnectionMode.Listen)
                    {
                        Assert.Equal(runner.DiagnosticPortPath, info.DiagnosticPortName);
                    }

                    Assert.Equal(5, info.Capabilities.Length); // Update if capabilities change

                    AssertCapability(enableInProcessFeatures, MonitorCapabilityConstants.Exceptions, info.Capabilities);
                    AssertCapability(enableInProcessFeatures, MonitorCapabilityConstants.ParameterCapturing, info.Capabilities);
                    AssertCapability(enableInProcessFeatures, MonitorCapabilityConstants.CallStacks, info.Capabilities);
                    AssertCapability(true, MonitorCapabilityConstants.Metrics, info.Capabilities);
                    AssertCapability(true, MonitorCapabilityConstants.HttpEgress, info.Capabilities);

                    await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                },
                configureTool: runner =>
                {
                    if (enableInProcessFeatures)
                    {
                        runner.ConfigurationFromEnvironment.EnableInProcessFeatures();
                        runner.ConfigurationFromEnvironment.EnableParameterCapturing();
                    }
                });
        }

        private static void AssertCapability(bool expectEnabled, string capabilityName, IMonitorCapability[] capabilities)
        {
            IMonitorCapability capability = capabilities.First(c => c.Name == capabilityName);
            Assert.NotNull(capability);
            Assert.Equal(expectEnabled, capability.Enabled);
        }
    }
}
