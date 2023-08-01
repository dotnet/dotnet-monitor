// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
                    int processId = await appRunner.ProcessIdTask;
                    //await appRunner.SendCommandAsync(TestAppScenarios.Exceptions.Commands.Begin);
                    //await appRunner.SendCommandAsync(TestAppScenarios.Exceptions.Commands.End);
                    await Task.Delay(5000); // TESTING ONLY

                    ResponseStreamHolder holder = await apiClient.CaptureExceptionsAsync(processId, WebApi.Exceptions.ExceptionsFormat.PlainText);
                    Assert.NotNull(holder);
                    StringBuilder builder = new();
                    using (var reader = new StreamReader(holder.Stream, Encoding.UTF8))
                    {
                        string value = reader.ReadToEnd();
                        builder.Append(value);
                        Console.WriteLine(value);
                    }
                    var fullString = builder.ToString();

                    /*
                    WebApi.Models.CallStackResult result = await JsonSerializer.DeserializeAsync<WebApi.Models.CallStackResult>(holder.Stream);
                    WebApi.Models.CallStackFrame[] expectedFrames = ExpectedFrames();
                    (WebApi.Models.CallStack stack, IList<WebApi.Models.CallStackFrame> actualFrames) = GetActualFrames(result, expectedFrames.First(), expectedFrames.Length);

                    Assert.NotNull(stack);
                    */

                    Assert.NotEmpty(fullString);
                    await appRunner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
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
                    int processId = await appRunner.ProcessIdTask;
                    //await appRunner.SendCommandAsync(TestAppScenarios.Exceptions.Commands.Begin);
                    //await appRunner.SendCommandAsync(TestAppScenarios.Exceptions.Commands.End);
                    await Task.Delay(3000); // TESTING ONLY -> loop maybe?

                    await appRunner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);

                    await Task.Delay(500); // TESTING ONLY

                    ResponseStreamHolder holder = await apiClient.CaptureExceptionsAsync(processId, WebApi.Exceptions.ExceptionsFormat.NewlineDelimitedJson);
                    Assert.NotNull(holder);

                    //string holderString = TEMP_GetString(holder.Stream);

                    //holderString.Trim();
                    //holderString.Substring(1, holderString.Length - 2); // remove open/close parentheses - hack

                    //byte[] byteArray = Encoding.UTF8.GetBytes(holderString);
                    //byte[] byteArray = Encoding.ASCII.GetBytes(contents);
                    //MemoryStream stream = new MemoryStream(byteArray);

                    var result = await JsonSerializer.DeserializeAsync<Dictionary<string, object>>(holder.Stream);

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
    }
}
