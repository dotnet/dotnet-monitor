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
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
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
    }
}
