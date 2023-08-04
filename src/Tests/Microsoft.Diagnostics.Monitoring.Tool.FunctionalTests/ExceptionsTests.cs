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
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    [Collection(DefaultCollectionFixture.Name)]
    public class ExceptionsTests
    {
        private const string FrameClassName = "Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios.ExceptionsScenario";
        private const string FrameMethodName = "ThrowAndCatchInvalidOperationException";
        private const string FrameParameterType = "System.Boolean";
        private const string FrameModuleName = "Microsoft.Diagnostics.Monitoring.UnitTestApp.dll";
        private const string ModuleName = "System.Private.CoreLib.dll";
        private const string ExceptionType = "System.InvalidOperationException";
        private const string ExceptionMessage = $"Exception of type '{ExceptionType}' was thrown.";

        private string exceptionsResult = string.Empty;

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITestOutputHelper _outputHelper;

        public ExceptionsTests(ITestOutputHelper outputHelper, ServiceProviderFixture serviceProviderFixture)
        {
            _httpClientFactory = serviceProviderFixture.ServiceProvider.GetService<IHttpClientFactory>();
            _outputHelper = outputHelper;
        }

        [Theory]
        [MemberData(nameof(ProfilerHelper.GetArchitecture), MemberType = typeof(ProfilerHelper))]
        public async Task ExceptionsTextTest(Architecture targetArchitecture)
        {
            await ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                DiagnosticPortConnectionMode.Listen,
                TestAppScenarios.Exceptions.Name,
                appValidate: async (appRunner, apiClient) =>
                {
                    await GetExceptions(apiClient, appRunner, ExceptionFormat.PlainText);

                    var exceptionsLines = exceptionsResult.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);

                    Assert.True(exceptionsLines.Length >= 4);
                    Assert.Contains("First chance exception at", exceptionsLines[0]);
                    Assert.Equal($"{ExceptionType}: {ExceptionMessage}", exceptionsLines[1]);
                    Assert.Equal($"   at {FrameClassName}.{FrameMethodName}({FrameParameterType},{FrameParameterType})", exceptionsLines[2]);
                    Assert.Equal($"   at {FrameClassName}.{FrameMethodName}()", exceptionsLines[3]);
                },
                configureApp: runner =>
                {
                    runner.Architecture = targetArchitecture;
                    runner.EnableMonitorStartupHook = true;
                },
                configureTool: runner =>
                {
                    runner.ConfigurationFromEnvironment.EnableInProcessFeatures();
                });
        }

        [Theory]
        [MemberData(nameof(ProfilerHelper.GetArchitecture), MemberType = typeof(ProfilerHelper))]
        public async Task ExceptionsWithEnvVarJsonTest(Architecture targetArchitecture)
        {
            await ExceptionsJsonTest(targetArchitecture, true);
        }

#if NET8_0_OR_GREATER
        [Theory]
        [MemberData(nameof(ProfilerHelper.GetArchitecture), MemberType = typeof(ProfilerHelper))]
        public async Task ExceptionsWithoutEnvVarJsonTest(Architecture targetArchitecture)
        {
            await ExceptionsJsonTest(targetArchitecture, false);
        }
#endif

        private async Task ExceptionsJsonTest(Architecture targetArchitecture, bool enableStartupHook)
        {
            await ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                DiagnosticPortConnectionMode.Listen,
                TestAppScenarios.Exceptions.Name,
                appValidate: async (appRunner, apiClient) =>
                {
                    DateTime startTime = DateTime.UtcNow.ToLocalTime();

                    await GetExceptions(apiClient, appRunner, ExceptionFormat.NewlineDelimitedJson);

                    DateTime currentTime = DateTime.UtcNow.ToLocalTime();

                    var exceptionsDict = JsonSerializer.Deserialize<Dictionary<string, object>>(exceptionsResult);

                    Assert.Equal("2", exceptionsDict["id"].ToString());
                    Assert.Equal(ExceptionType, exceptionsDict["typeName"].ToString());

                    var timestamp = DateTime.Parse(exceptionsDict["timestamp"].ToString());
                    Assert.True(startTime < timestamp);
                    Assert.True(currentTime > timestamp);
                    Assert.Equal(ModuleName, exceptionsDict["moduleName"].ToString());
                    Assert.Equal(ExceptionMessage, exceptionsDict["message"].ToString());

                    var callStackResultsRootElement = JsonSerializer.SerializeToDocument(exceptionsDict["callStack"]).RootElement;

                    Assert.NotEqual("0", callStackResultsRootElement.GetProperty("threadId").ToString());

                    var topFrame = callStackResultsRootElement.GetProperty("frames").EnumerateArray().FirstOrDefault();

                    Assert.Equal(FrameMethodName, topFrame.GetProperty("methodName").ToString());
                    Assert.Equal(2, topFrame.GetProperty("parameterTypes").GetArrayLength());
                    Assert.Equal(FrameParameterType, topFrame.GetProperty("parameterTypes")[0].ToString());
                    Assert.Equal(FrameParameterType, topFrame.GetProperty("parameterTypes")[1].ToString());
                    Assert.Equal(FrameClassName, topFrame.GetProperty("className").ToString());
                    Assert.Equal(FrameModuleName, topFrame.GetProperty("moduleName").ToString());
                },
                configureApp: runner =>
                {
                    runner.Architecture = targetArchitecture;
                    runner.EnableMonitorStartupHook = enableStartupHook;
                },
                configureTool: runner =>
                {
                    runner.ConfigurationFromEnvironment.EnableInProcessFeatures();
                });
        }

        private async Task GetExceptions(ApiClient apiClient, AppRunner appRunner, ExceptionFormat format)
        {
            await appRunner.SendCommandAsync(TestAppScenarios.Exceptions.Commands.Begin);

            int processId = await appRunner.ProcessIdTask;

            await RetryUtilities.RetryAsync(
                () => CaptureExtensions(apiClient, processId, format),
                shouldRetry: (Exception ex) => ex is ArgumentException,
                maxRetryCount: 5,
                outputHelper: _outputHelper);
        }

        private async Task CaptureExtensions(ApiClient apiClient, int processId, ExceptionFormat format)
        {
            await Task.Delay(500);

            ResponseStreamHolder holder = await apiClient.CaptureExceptionsAsync(processId, format);

            using (var reader = new StreamReader(holder.Stream, Encoding.UTF8))
            {
                exceptionsResult = reader.ReadToEnd();
            }

            if (string.IsNullOrEmpty(exceptionsResult))
            {
                throw new ArgumentException();
            }
        }
    }
}
