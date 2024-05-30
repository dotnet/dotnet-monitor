// Licensed to the .NET Foundation under one or more agreements.
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
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
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
    public class ParameterCapturingTests : IDisposable
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITestOutputHelper _outputHelper;
        private readonly TemporaryDirectory _tempDirectory;

        private const string FileProviderName = "files";

        public ParameterCapturingTests(ITestOutputHelper outputHelper, ServiceProviderFixture serviceProviderFixture)
        {
            _httpClientFactory = serviceProviderFixture.ServiceProvider.GetService<IHttpClientFactory>();
            _outputHelper = outputHelper;
            _tempDirectory = new(outputHelper);
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

                ValidationProblemDetailsException validationException = await Assert.ThrowsAsync<ValidationProblemDetailsException>(() => apiClient.CaptureParametersAsync(processId, Timeout.InfiniteTimeSpan, config));
                Assert.Equal(HttpStatusCode.BadRequest, validationException.StatusCode);

                await appRunner.SendCommandAsync(TestAppScenarios.ParameterCapturing.Commands.Continue);
            });
        }

        [Theory]
        [MemberData(nameof(ProfilerHelper.GetArchitecture), MemberType = typeof(ProfilerHelper))]
        public Task CapturesParametersInNonAspNetApps(Architecture targetArchitecture) =>
            CapturesParametersCore(TestAppScenarios.ParameterCapturing.SubScenarios.NonAspNetApp, targetArchitecture, CapturedParameterFormat.JsonSequence);

        [Theory]
        [MemberData(nameof(ProfilerHelper.GetArchitecture), MemberType = typeof(ProfilerHelper))]
        public Task CapturesParametersAndOutputJsonSequence(Architecture targetArchitecture) =>
                CapturesParametersCore(TestAppScenarios.ParameterCapturing.SubScenarios.AspNetApp, targetArchitecture, CapturedParameterFormat.JsonSequence);

        [Theory]
        [MemberData(nameof(ProfilerHelper.GetArchitecture), MemberType = typeof(ProfilerHelper))]
        public Task CapturesParametersAndOutputNewlineDelimitedJson(Architecture targetArchitecture) =>
                CapturesParametersCore(TestAppScenarios.ParameterCapturing.SubScenarios.AspNetApp, targetArchitecture, CapturedParameterFormat.NewlineDelimitedJson);

#else // NET7_0_OR_GREATER
        [Theory]
        [MemberData(nameof(ProfilerHelper.GetArchitecture), MemberType = typeof(ProfilerHelper))]
        public async Task Net6AppFailsOperation(Architecture targetArchitecture)
        {
            await RunTestCaseCore(TestAppScenarios.ParameterCapturing.SubScenarios.AspNetApp, targetArchitecture, async (appRunner, apiClient) =>
            {
                int processId = await appRunner.ProcessIdTask;

                CaptureParametersConfiguration config = GetValidConfiguration();

                ValidationProblemDetailsException validationException = await Assert.ThrowsAsync<ValidationProblemDetailsException>(() => apiClient.CaptureParametersAsync(processId, TimeSpan.FromSeconds(1), config));
                Assert.Equal(HttpStatusCode.BadRequest, validationException.StatusCode);

                await appRunner.SendCommandAsync(TestAppScenarios.ParameterCapturing.Commands.Continue);
            });
        }
#endif // NET7_0_OR_GREATER

        private async Task CapturesParametersCore(string subScenarioName,Architecture targetArchitecture, CapturedParameterFormat format)
        {
            await RunTestCaseCore(subScenarioName, targetArchitecture, async (appRunner, apiClient) =>
            {
                int processId = await appRunner.ProcessIdTask;

                CaptureParametersConfiguration config = GetValidConfiguration();
                MethodDescription expectedCapturedMethod = config.Methods[0];

                OperationResponse response = await apiClient.CaptureParametersAsync(processId, TimeSpan.FromSeconds(2), config, format, FileProviderName);
                Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

                OperationStatusResponse _ = await apiClient.WaitForOperationToStart(response.OperationUri);

                await appRunner.SendCommandAsync(TestAppScenarios.ParameterCapturing.Commands.Continue);

                OperationStatusResponse operationResult = await apiClient.PollOperationToCompletion(response.OperationUri);
                Assert.Equal(HttpStatusCode.Created, operationResult.StatusCode);
                Assert.Equal(OperationState.Succeeded, operationResult.OperationStatus.Status);

                Assert.NotNull(operationResult.OperationStatus.ResourceLocation);
                Assert.True(File.Exists(operationResult.OperationStatus.ResourceLocation));
                using FileStream resultStream = new(operationResult.OperationStatus.ResourceLocation, FileMode.Open);

                List<CapturedMethod> capturedMethods = await DeserializeCapturedMethodsAsync(resultStream, format);

                Assert.NotNull(capturedMethods);
                CapturedMethod actualMethod = Assert.Single(capturedMethods);

                Assert.Equal(expectedCapturedMethod.TypeName, actualMethod.TypeName);
                Assert.Equal(expectedCapturedMethod.MethodName, actualMethod.MethodName);
            });
        }

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
                    toolRunner.WriteKeyPerValueConfiguration(new RootOptions().AddFileSystemEgress(FileProviderName, _tempDirectory.FullName));
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

        public void Dispose()
        {
            _tempDirectory.Dispose();
        }

        private static async Task<List<CapturedMethod>> DeserializeCapturedMethodsAsync(Stream inputStream, CapturedParameterFormat format)
        {
            List<CapturedMethod> capturedMethods = [];
            JsonSerializerOptions options = new();
            options.Converters.Add(new JsonStringEnumConverter());

            using StreamReader reader = new StreamReader(inputStream);

            string line;
            while (null != (line = await reader.ReadLineAsync()))
            {
                if (format == CapturedParameterFormat.JsonSequence)
                {
                    Assert.True(line.Length > 1);
                    Assert.Equal(JsonSequenceRecordSeparator, line[0]);
                    Assert.NotEqual(JsonSequenceRecordSeparator, line[1]);

                    line = line.TrimStart(JsonSequenceRecordSeparator);
                }

                capturedMethods.Add(JsonSerializer.Deserialize<CapturedMethod>(line, options));
            }
            return capturedMethods;
        }

        private const char JsonSequenceRecordSeparator = '\u001E';
    }
}
