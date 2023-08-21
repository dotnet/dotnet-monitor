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
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.HttpApi;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using System;
using System.Threading;
using System.Net;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    [Collection(DefaultCollectionFixture.Name)]
    public class ParameterCapturingTests
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITestOutputHelper _outputHelper;

        public ParameterCapturingTests(ITestOutputHelper outputHelper, ServiceProviderFixture serviceProviderFixture)
        {
            _httpClientFactory = serviceProviderFixture.ServiceProvider.GetService<IHttpClientFactory>();
            _outputHelper = outputHelper;
        }

#if NET7_0_OR_GREATER
        [Theory]
        [MemberData(nameof(ProfilerHelper.GetArchitecture), MemberType = typeof(ProfilerHelper))]
        public async Task UnresolvableMethodsFailsOperation(Architecture targetArchitecture)
        {
            await RunTestCaseCore(TestAppScenarios.ParameterCapturing.SubScenarios.AspNetApp, targetArchitecture, async (appRunner, apiClient) =>
            {
                int processId = await appRunner.ProcessIdTask;

                MethodDescription[] methods = new MethodDescription[]
                {
                    new MethodDescription()
                    {
                        AssemblyName = Guid.NewGuid().ToString("D"),
                        TypeName = Guid.NewGuid().ToString("D"),
                        MethodName = Guid.NewGuid().ToString("D")
                    }
                };

                OperationResponse response = await apiClient.CaptureParametersAsync(processId, Timeout.InfiniteTimeSpan, methods);
                Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

                OperationStatusResponse operationResult = await apiClient.PollOperationToCompletion(response.OperationUri);
                Assert.Equal(HttpStatusCode.OK, operationResult.StatusCode);
                Assert.Equal(OperationState.Failed, operationResult.OperationStatus.Status);

                await appRunner.SendCommandAsync(TestAppScenarios.ParameterCapturing.Commands.Continue);
            });
        }

        [Theory]
        [MemberData(nameof(ProfilerHelper.GetArchitecture), MemberType = typeof(ProfilerHelper))]
        public async Task NonAspNetAppFailsOperation(Architecture targetArchitecture)
        {
            await RunTestCaseCore(TestAppScenarios.ParameterCapturing.SubScenarios.NonAspNetApp, targetArchitecture, async (appRunner, apiClient) =>
            {
                int processId = await appRunner.ProcessIdTask;

                MethodDescription[] methods = GetValidConfiguration();

                OperationResponse response = await apiClient.CaptureParametersAsync(processId, Timeout.InfiniteTimeSpan, methods);
                Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

                OperationStatusResponse operationResult = await apiClient.PollOperationToCompletion(response.OperationUri);
                Assert.Equal(HttpStatusCode.OK, operationResult.StatusCode);
                Assert.Equal(OperationState.Failed, operationResult.OperationStatus.Status);

                await appRunner.SendCommandAsync(TestAppScenarios.ParameterCapturing.Commands.Continue);
            });
        }

        [Theory]
        [MemberData(nameof(ProfilerHelper.GetArchitecture), MemberType = typeof(ProfilerHelper))]
        public async Task DoesProduceLogStatements(Architecture targetArchitecture)
        {
            await RunTestCaseCore(TestAppScenarios.ParameterCapturing.SubScenarios.ExpectLogStatement, targetArchitecture, async (appRunner, apiClient) =>
            {
                int processId = await appRunner.ProcessIdTask;

                MethodDescription[] methods = GetValidConfiguration();

                OperationResponse response = await apiClient.CaptureParametersAsync(processId, Timeout.InfiniteTimeSpan, methods);
                Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

                OperationStatusResponse operationStatus = await apiClient.WaitForOperationToStart(response.OperationUri);
                Assert.Equal(OperationState.Running, operationStatus.OperationStatus.Status);

                await appRunner.SendCommandAsync(TestAppScenarios.ParameterCapturing.Commands.Continue);
            });
        }

        [Theory]
        [MemberData(nameof(ProfilerHelper.GetArchitecture), MemberType = typeof(ProfilerHelper))]
        public async Task StopsProducingLogStatementsAfterOperationCompleted(Architecture targetArchitecture)
        {
            await RunTestCaseCore(TestAppScenarios.ParameterCapturing.SubScenarios.DoNotExpectLogStatement, targetArchitecture, async (appRunner, apiClient) =>
            {
                int processId = await appRunner.ProcessIdTask;

                MethodDescription[] methods = GetValidConfiguration();

                OperationResponse response = await apiClient.CaptureParametersAsync(processId, TimeSpan.FromSeconds(2), methods);
                Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

                OperationStatusResponse operationResult = await apiClient.PollOperationToCompletion(response.OperationUri);
                Assert.Equal(HttpStatusCode.Created, operationResult.StatusCode);
                Assert.Equal(OperationState.Succeeded, operationResult.OperationStatus.Status);

                await appRunner.SendCommandAsync(TestAppScenarios.ParameterCapturing.Commands.Continue);
            });
        }
#else // !NET7_0_OR_GREATER
        [Theory(Skip = "Pending https://github.com/dotnet/dotnet-monitor/pull/5169")]
        [MemberData(nameof(ProfilerHelper.GetArchitecture), MemberType = typeof(ProfilerHelper))]
        public async Task Net6AppFailsOperation(Architecture targetArchitecture)
        {
            await RunTestCaseCore(TestAppScenarios.ParameterCapturing.SubScenarios.AspNetApp, targetArchitecture, async (appRunner, apiClient) =>
            {
                int processId = await appRunner.ProcessIdTask;

                MethodDescription[] methods = GetValidConfiguration();

                OperationResponse response = await apiClient.CaptureParametersAsync(processId, TimeSpan.FromSeconds(1), methods);
                Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

                OperationStatusResponse operationResult = await apiClient.PollOperationToCompletion(response.OperationUri);
                Assert.Equal(HttpStatusCode.OK, operationResult.StatusCode);
                Assert.Equal(OperationState.Failed, operationResult.OperationStatus.Status);

                await appRunner.SendCommandAsync(TestAppScenarios.ParameterCapturing.Commands.Continue);
            });
        }
#endif // !NET7_0_OR_GREATER

        private async Task RunTestCaseCore(string subScenarioName, Architecture targetArchitecture, Func<AppRunner, ApiClient, Task> appValidate)
        {
            await ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                DiagnosticPortConnectionMode.Listen,
                TestAppScenarios.ParameterCapturing.Name,
                appValidate: appValidate,
                configureApp: runner =>
                {
                    runner.EnableMonitorStartupHook = true;
                    runner.Architecture = targetArchitecture;
                },
                configureTool: (toolRunner) =>
                {
                    toolRunner.ConfigurationFromEnvironment.EnableInProcessFeatures();
                    toolRunner.ConfigurationFromEnvironment.InProcessFeatures.ParameterCapturing = new()
                    {
                        Enabled = true
                    };
                },
                profilerLogLevel: LogLevel.Trace,
                subScenarioName: subScenarioName);
        }

        private static MethodDescription[] GetValidConfiguration()
        {
            return new MethodDescription[]
            {
                    new MethodDescription()
                    {
                        AssemblyName = "Microsoft.Diagnostics.Monitoring.UnitTestApp",
                        TypeName = "SampleMethods.StaticTestMethodSignatures",
                        MethodName = "Basic"
                    }
            };
        }

    }
}
