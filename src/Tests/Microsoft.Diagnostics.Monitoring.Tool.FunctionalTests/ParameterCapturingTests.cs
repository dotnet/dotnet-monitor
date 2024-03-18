// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Fixtures;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.HttpApi;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Runners;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
#if NET7_0_OR_GREATER
using System.Threading;
#endif
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

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

                CaptureParametersConfiguration config = new()
                {
                    Methods = new MethodDescription[]
                    {
                        new MethodDescription()
                        {
                            ModuleName = Guid.NewGuid().ToString("D"),
                            TypeName = Guid.NewGuid().ToString("D"),
                            MethodName = Guid.NewGuid().ToString("D")
                        }
                    }
                };

                OperationResponse response = await apiClient.CaptureParametersAsync(processId, Timeout.InfiniteTimeSpan, config);
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

                CaptureParametersConfiguration config = GetValidConfiguration();

                OperationResponse response = await apiClient.CaptureParametersAsync(processId, Timeout.InfiniteTimeSpan, config);
                Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

                OperationStatusResponse operationResult = await apiClient.PollOperationToCompletion(response.OperationUri);
                Assert.Equal(HttpStatusCode.OK, operationResult.StatusCode);
                Assert.Equal(OperationState.Failed, operationResult.OperationStatus.Status);

                await appRunner.SendCommandAsync(TestAppScenarios.ParameterCapturing.Commands.Continue);
            });
        }

        [Theory]
        [MemberData(nameof(ProfilerHelper.GetArchitecture), MemberType = typeof(ProfilerHelper))]
        public async Task DoesntProduceLogStatements(Architecture targetArchitecture)
        {
            await RunTestCaseCore(TestAppScenarios.ParameterCapturing.SubScenarios.DoNotExpectLogStatement, targetArchitecture, async (appRunner, apiClient) =>
            {
                int processId = await appRunner.ProcessIdTask;

                CaptureParametersConfiguration config = GetValidConfiguration();

                OperationResponse response = await apiClient.CaptureParametersAsync(processId, Timeout.InfiniteTimeSpan, config);
                Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

                OperationStatusResponse operationStatus = await apiClient.WaitForOperationToStart(response.OperationUri);
                Assert.Equal(OperationState.Running, operationStatus.OperationStatus.Status);

                await appRunner.SendCommandAsync(TestAppScenarios.ParameterCapturing.Commands.Validate);
            });
        }

        [Theory]
        [MemberData(nameof(ProfilerHelper.GetArchitecture), MemberType = typeof(ProfilerHelper))]
        public async Task StopsProducingLogStatementsAfterOperationCompletes(Architecture targetArchitecture)
        {
            await RunTestCaseCore(TestAppScenarios.ParameterCapturing.SubScenarios.DoNotExpectLogStatement, targetArchitecture, async (appRunner, apiClient) =>
            {
                int processId = await appRunner.ProcessIdTask;

                CaptureParametersConfiguration config = GetValidConfiguration();

                OperationResponse response = await apiClient.CaptureParametersAsync(processId, TimeSpan.FromSeconds(2), config);
                Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

                OperationStatusResponse operationResult = await apiClient.PollOperationToCompletion(response.OperationUri);
                Assert.Equal(HttpStatusCode.Created, operationResult.StatusCode);
                Assert.Equal(OperationState.Succeeded, operationResult.OperationStatus.Status);

                await appRunner.SendCommandAsync(TestAppScenarios.ParameterCapturing.Commands.Validate);
            });
        }
#else // NET7_0_OR_GREATER
        [Theory]
        [MemberData(nameof(ProfilerHelper.GetArchitecture), MemberType = typeof(ProfilerHelper))]
        public async Task Net6AppFailsOperation(Architecture targetArchitecture)
        {
            await RunTestCaseCore(TestAppScenarios.ParameterCapturing.SubScenarios.AspNetApp, targetArchitecture, async (appRunner, apiClient) =>
            {
                int processId = await appRunner.ProcessIdTask;

                CaptureParametersConfiguration config = GetValidConfiguration();

                OperationResponse response = await apiClient.CaptureParametersAsync(processId, TimeSpan.FromSeconds(1), config);
                Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

                OperationStatusResponse operationResult = await apiClient.PollOperationToCompletion(response.OperationUri);
                Assert.Equal(HttpStatusCode.OK, operationResult.StatusCode);
                Assert.Equal(OperationState.Failed, operationResult.OperationStatus.Status);

                await appRunner.SendCommandAsync(TestAppScenarios.ParameterCapturing.Commands.Continue);
            });
        }
#endif // NET7_0_OR_GREATER

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

        private static CaptureParametersConfiguration GetValidConfiguration()
        {
            return new CaptureParametersConfiguration()
            {
                Methods = new MethodDescription[]
                {
                    new MethodDescription()
                    {
                        ModuleName = "Microsoft.Diagnostics.Monitoring.UnitTestApp.dll",
                        TypeName = "SampleMethods.StaticTestMethodSignatures",
                        MethodName = "NoArgs"
                    }
                }
            };
        }

    }
}
