// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Fixtures;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.HttpApi;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Runners;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Exceptions;
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
    public class ExceptionTests
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITestOutputHelper _outputHelper;

        public ExceptionTests(ITestOutputHelper outputHelper, ServiceProviderFixture serviceProviderFixture)
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
                    string exceptionsResult = await GetExceptions(apiClient, appRunner, ExceptionsFormat.PlainText);

                    Assert.NotEmpty(exceptionsResult);
                },
                configureApp: runner =>
                {
                    runner.Architecture = targetArchitecture;
                },
                configureTool: runner =>
                {
                    runner.ConfigurationFromEnvironment.EnableInProcessFeatures();
                });
        }

        [Theory]
        [MemberData(nameof(ProfilerHelper.GetArchitecture), MemberType = typeof(ProfilerHelper))]
        public async Task ExceptionsJsonTest(Architecture targetArchitecture)
        {
            await ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                DiagnosticPortConnectionMode.Listen,
                TestAppScenarios.Exceptions.Name,
                appValidate: async (appRunner, apiClient) =>
                {
                    DateTime startTime = DateTime.UtcNow.ToLocalTime();

                    string exceptionsResult = await GetExceptions(apiClient, appRunner, ExceptionsFormat.NewlineDelimitedJson);

                    var result = JsonSerializer.Deserialize<Dictionary<string, object>>(exceptionsResult);
                    //var result = await JsonSerializer.DeserializeAsync<Dictionary<string, object>>(holder.Stream);

                    Assert.Equal("2", result["id"].ToString());
                    Assert.Equal("System.InvalidOperationException", result["typeName"].ToString());
                    Assert.True(startTime < DateTime.Parse(result["timestamp"].ToString()));
                    Assert.Equal("System.Private.CoreLib.dll", result["moduleName"].ToString());
                    Assert.Equal("Exception of type 'System.InvalidOperationException' was thrown.", result["message"].ToString());

                    var callStackResultsRootElement = JsonSerializer.SerializeToDocument(result["callStack"]).RootElement;

                    Assert.NotEqual("0", callStackResultsRootElement.GetProperty("threadId").ToString());
                    //Assert.NotNull(callStackResultsRootElement.GetProperty("threadName").ToString()); // No value is currently being set

                    var topFrame = callStackResultsRootElement.GetProperty("frames").EnumerateArray().FirstOrDefault();

                    Assert.Equal("ThrowAndCatchInvalidOperationException", topFrame.GetProperty("methodName").ToString());
                    Assert.Equal(2, topFrame.GetProperty("parameterTypes").GetArrayLength());
                    Assert.Equal("System.Boolean", topFrame.GetProperty("parameterTypes")[0].ToString());
                    Assert.Equal("System.Boolean", topFrame.GetProperty("parameterTypes")[1].ToString());
                    Assert.Equal("Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios.ExceptionsScenario", topFrame.GetProperty("className").ToString());
                    Assert.Equal("Microsoft.Diagnostics.Monitoring.UnitTestApp.dll", topFrame.GetProperty("moduleName").ToString());
                },
                configureApp: runner =>
                {
                    runner.Architecture = targetArchitecture;
                },
                configureTool: runner =>
                {
                    runner.ConfigurationFromEnvironment.EnableInProcessFeatures();
                });
        }

        private static string TEMP_GetString(Stream stream)
        {
            StringBuilder builder = new();
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                string value = reader.ReadToEnd();
                builder.Append(value);
                Console.WriteLine(value);
            }
            return builder.ToString();
        }

        private static async Task<string> GetExceptions(ApiClient apiClient, AppRunner appRunner, ExceptionsFormat format)
        {
            await appRunner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);

            int processId = await appRunner.ProcessIdTask;

            const int retryMaxCount = 5;
            string holderStreamString = string.Empty;
            int retryCounter = 0;
            while (string.IsNullOrEmpty(holderStreamString) && retryCounter < retryMaxCount)
            {
                await Task.Delay(500);

                ResponseStreamHolder holder = await apiClient.CaptureExceptionsAsync(processId, format);

                holderStreamString = TEMP_GetString(holder.Stream);

                ++retryCounter;
            }

            Assert.NotEqual(retryMaxCount, retryCounter);

            return holderStreamString;
        }
    }
}
