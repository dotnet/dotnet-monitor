// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Fixtures;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.HttpApi;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Runners;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    [Collection(DefaultCollectionFixture.Name)]
    public class StacksTests
    {
#if NET6_0_OR_GREATER

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITestOutputHelper _outputHelper;
        private readonly TemporaryDirectory _tempDirectory;

        private const string ExpectedModule = @"Microsoft.Diagnostics.Monitoring.UnitTestApp.dll";
        private const string ExpectedClass = @"Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios.StacksWorker+StacksWorkerNested`1[System.Int32]";
        private const string ExpectedFunction = @"DoWork[System.Int64]";
        private const string ExpectedCallbackFunction = @"Callback";
        private const string NativeFrame = "[NativeFrame]";

        public StacksTests(ITestOutputHelper outputHelper, ServiceProviderFixture serviceProviderFixture)
        {
            _httpClientFactory = serviceProviderFixture.ServiceProvider.GetService<IHttpClientFactory>();
            _outputHelper = outputHelper;
            _tempDirectory = new TemporaryDirectory(_outputHelper);
        }

        [Theory]
        [MemberData(nameof(ProfilerHelper.GetArchitecture), MemberType = typeof(ProfilerHelper))]
        public Task TestPlainTextStacks(Architecture targetArchitecture)
        {
            return ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                WebApi.DiagnosticPortConnectionMode.Listen,
                TestAppScenarios.Stacks.Name,
                appValidate: async (runner, client) =>
                {
                    // Wait for the process to be discovered.
                    int processId = await runner.ProcessIdTask;
                    _ = await client.GetProcessWithRetryAsync(_outputHelper, pid: processId);

                    using ResponseStreamHolder holder = await client.CaptureStacksAsync(processId, plainText: true);
                    Assert.NotNull(holder);

                    using StreamReader reader = new StreamReader(holder.Stream);
                    string line = null;

                    string[] expectedFrames =
                    {
                        FormatFrame(ExpectedModule, ExpectedClass, ExpectedCallbackFunction),
                        NativeFrame,
                        FormatFrame(ExpectedModule, ExpectedClass, ExpectedFunction),
                    };

                    var actualFrames = new List<string>();

                    while ((line = reader.ReadLine()) != null)
                    {
                        line = line.TrimStart();
                        if (actualFrames.Count == expectedFrames.Length)
                        {
                            break;
                        }
                        if ((line == expectedFrames.First()) || (actualFrames.Count > 0))
                        {
                            actualFrames.Add(line);
                        }
                    }

                    Assert.Equal(expectedFrames, actualFrames);

                    await runner.SendCommandAsync(TestAppScenarios.Stacks.Commands.Continue);
                },
                configureApp: runner =>
                {
                    runner.Architecture = targetArchitecture;
                },
                configureTool: runner =>
                {
                    runner.ConfigurationFromEnvironment.EnableInProcessFeatures();
                    runner.EnableCallStacksFeature = true;
                });
        }

        [Theory]
        [MemberData(nameof(ProfilerHelper.GetArchitecture), MemberType = typeof(ProfilerHelper))]
        public Task TestJsonStacks(Architecture targetArchitecture)
        {
            return ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                WebApi.DiagnosticPortConnectionMode.Listen,
                TestAppScenarios.Stacks.Name,
                appValidate: async (runner, client) =>
                {
                    // Wait for the process to be discovered.
                    int processId = await runner.ProcessIdTask;
                    _ = await client.GetProcessWithRetryAsync(_outputHelper, pid: processId);

                    using ResponseStreamHolder holder = await client.CaptureStacksAsync(processId, plainText: false);
                    Assert.NotNull(holder);

                    WebApi.Models.CallStackResult result = await JsonSerializer.DeserializeAsync<WebApi.Models.CallStackResult>(holder.Stream);
                    WebApi.Models.CallStackFrame[] expectedFrames = ExpectedFrames();
                    IList<WebApi.Models.CallStackFrame> actualFrames = GetActualFrames(result, expectedFrames.First(), expectedFrames.Length);

                    Assert.Equal(expectedFrames.Length, actualFrames.Count);
                    for (int i = 0; i < expectedFrames.Length; i++)
                    {
                        Assert.True(AreFramesEqual(expectedFrames[i], actualFrames[i]));
                    }

                    await runner.SendCommandAsync(TestAppScenarios.Stacks.Commands.Continue);
                },
                configureApp: runner =>
                {
                    runner.Architecture = targetArchitecture;
                },
                configureTool: runner =>
                {
                    runner.ConfigurationFromEnvironment.EnableInProcessFeatures();
                    runner.EnableCallStacksFeature = true;
                });
        }


        [Theory]
        [MemberData(nameof(ProfilerHelper.GetArchitecture), MemberType = typeof(ProfilerHelper))]
        public Task TestRepeatStackCalls(Architecture targetArchitecture)
        {
            return ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                WebApi.DiagnosticPortConnectionMode.Listen,
                TestAppScenarios.Stacks.Name,
                appValidate: async (runner, client) =>
                {
                    // Wait for the process to be discovered.
                    int processId = await runner.ProcessIdTask;
                    _ = await client.GetProcessWithRetryAsync(_outputHelper, pid: processId);

                    using ResponseStreamHolder holder1 = await client.CaptureStacksAsync(processId, plainText: false);
                    Assert.NotNull(holder1);

                    WebApi.Models.CallStackResult result1 = await JsonSerializer.DeserializeAsync<WebApi.Models.CallStackResult>(holder1.Stream);

                    using ResponseStreamHolder holder2 = await client.CaptureStacksAsync(processId, WebApi.StackFormat.Json);
                    Assert.NotNull(holder2);

                    WebApi.Models.CallStackResult result2 = await JsonSerializer.DeserializeAsync<WebApi.Models.CallStackResult>(holder2.Stream);

                    Assert.NotEmpty(result1.Stacks);
                    Assert.NotEmpty(result2.Stacks);

                    Assert.NotEmpty(result1.Stacks.SelectMany(s => s.Frames));
                    Assert.NotEmpty(result2.Stacks.SelectMany(s => s.Frames));


                    await runner.SendCommandAsync(TestAppScenarios.Stacks.Commands.Continue);
                },
                configureApp: runner =>
                {
                    runner.Architecture = targetArchitecture;
                },
                configureTool: runner =>
                {
                    runner.ConfigurationFromEnvironment.EnableInProcessFeatures();
                    runner.EnableCallStacksFeature = true;
                });
        }

        /// <summary>
        /// Verifies that the /stacks route returns 404 if the stacks feature is not enabled.
        /// </summary>
        [Theory]
        [MemberData(nameof(ProfilerHelper.GetArchitecture), MemberType = typeof(ProfilerHelper))]
        public Task TestFeatureNotEnabled(Architecture targetArchitecture)
        {
            return ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                WebApi.DiagnosticPortConnectionMode.Listen,
                TestAppScenarios.Stacks.Name,
                appValidate: async (runner, client) =>
                {
                    // Wait for the process to be discovered.
                    int processId = await runner.ProcessIdTask;
                    _ = await client.GetProcessWithRetryAsync(_outputHelper, pid: processId);

                    ApiStatusCodeException ex = await Assert.ThrowsAsync<ApiStatusCodeException>(() => client.CaptureStacksAsync(processId, plainText: false));
                    Assert.Equal(HttpStatusCode.NotFound, ex.StatusCode);

                    await runner.SendCommandAsync(TestAppScenarios.Stacks.Commands.Continue);
                },
                configureApp: runner =>
                {
                    runner.Architecture = targetArchitecture;
                },
                configureTool: runner =>
                {
                    runner.ConfigurationFromEnvironment.EnableInProcessFeatures();
                    // Note that the Stacks experimental feature is not enabled
                });
        }

        /// <summary>
        /// Verifies that the /stacks route returns 404 if the in-process features are not enabled.
        /// </summary>
        [Theory]
        [MemberData(nameof(ProfilerHelper.GetArchitecture), MemberType = typeof(ProfilerHelper))]
        public Task TestInProcessFeaturesNotEnabled(Architecture targetArchitecture)
        {
            return ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                WebApi.DiagnosticPortConnectionMode.Listen,
                TestAppScenarios.Stacks.Name,
                appValidate: async (runner, client) =>
                {
                    // Wait for the process to be discovered.
                    int processId = await runner.ProcessIdTask;
                    _ = await client.GetProcessWithRetryAsync(_outputHelper, pid: processId);

                    ApiStatusCodeException ex = await Assert.ThrowsAsync<ApiStatusCodeException>(() => client.CaptureStacksAsync(processId, plainText: false));
                    Assert.Equal(HttpStatusCode.NotFound, ex.StatusCode);

                    await runner.SendCommandAsync(TestAppScenarios.Stacks.Commands.Continue);
                },
                configureApp: runner =>
                {
                    runner.Architecture = targetArchitecture;
                },
                configureTool: runner =>
                {
                    runner.EnableCallStacksFeature = true;
                    // Note that the in-process features are not enabled
                });
        }

        [Theory(Skip = "Disable unstable tests.")]
        [MemberData(nameof(ProfilerHelper.GetArchitecture), MemberType = typeof(ProfilerHelper))]
        public Task TestCollectStacksAction(Architecture targetArchitecture)
        {
            Task ruleCompletedTask = null;

            return ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                WebApi.DiagnosticPortConnectionMode.Listen,
                TestAppScenarios.Stacks.Name,
                appValidate: async (runner, client) =>
                {
                    await ruleCompletedTask;

                    string[] files = Directory.GetFiles(_tempDirectory.FullName, "*");
                    Assert.Single(files);
                    using FileStream stream = File.OpenRead(files.First());

                    WebApi.Models.CallStackResult result = await JsonSerializer.DeserializeAsync<WebApi.Models.CallStackResult>(stream);
                    WebApi.Models.CallStackFrame[] expectedFrames = ExpectedFrames();
                    IList<WebApi.Models.CallStackFrame> actualFrames = GetActualFrames(result, expectedFrames.First(), expectedFrames.Length);

                    Assert.Equal(expectedFrames.Length, actualFrames.Count);
                    for (int i = 0; i < expectedFrames.Length; i++)
                    {
                        Assert.True(AreFramesEqual(expectedFrames[i], actualFrames[i]));
                    }

                    await runner.SendCommandAsync(TestAppScenarios.Stacks.Commands.Continue);
                },
                configureApp: runner =>
                {
                    runner.Architecture = targetArchitecture;
                },
                configureTool: runner =>
                {
                    const string fileEgress = nameof(fileEgress);
                    runner.EnableCallStacksFeature = true;
                    runner.ConfigurationFromEnvironment
                        .EnableInProcessFeatures()
                        .AddFileSystemEgress(fileEgress, _tempDirectory.FullName)
                        .CreateCollectionRule("StacksCounterRule")
                        .SetEventCounterTrigger(options =>
                        {
                            options.ProviderName = "StackScenario";
                            options.CounterName = "Ready";
                            options.GreaterThan = 0.0;
                            options.SlidingWindowDuration = TimeSpan.FromSeconds(5);
                        })
                        .AddCollectStacksAction(fileEgress, Tools.Monitor.CollectionRules.Options.Actions.CallStackFormat.Json);

                    ruleCompletedTask = runner.WaitForCollectionRuleActionsCompletedAsync("StacksCounterRule");
                });
        }

        private static string FormatFrame(string module, string @class, string function) =>
            FormattableString.Invariant($"{module}!{@class}.{function}");

        private static bool AreFramesEqual(WebApi.Models.CallStackFrame left, WebApi.Models.CallStackFrame right) =>
            (left.ModuleName == right.ModuleName) && (left.ClassName == right.ClassName) && (left.MethodName == right.MethodName);

        private static IList<WebApi.Models.CallStackFrame> GetActualFrames(WebApi.Models.CallStackResult result, WebApi.Models.CallStackFrame expectedFirstFrame, int expectedFrameCount)
        {
            var actualFrames = new List<WebApi.Models.CallStackFrame>();
            foreach (WebApi.Models.CallStack stack in result.Stacks)
            {
                actualFrames.Clear();
                foreach (var frame in stack.Frames)
                {
                    if (AreFramesEqual(expectedFirstFrame, frame) || actualFrames.Count > 0)
                    {
                        actualFrames.Add(frame);
                        if (actualFrames.Count == expectedFrameCount)
                        {
                            return actualFrames;
                        }
                    }
                }
            }
            return actualFrames;
        }

        private static WebApi.Models.CallStackFrame[] ExpectedFrames() => new WebApi.Models.CallStackFrame[]
            {
                        new WebApi.Models.CallStackFrame
                        {
                            ModuleName = ExpectedModule,
                            ClassName = ExpectedClass,
                            MethodName = ExpectedCallbackFunction
                        },
                        new WebApi.Models.CallStackFrame
                        {
                            ModuleName = NativeFrame,
                            ClassName = NativeFrame,
                            MethodName = NativeFrame
                        },
                        new WebApi.Models.CallStackFrame
                        {
                            ModuleName = ExpectedModule,
                            ClassName = ExpectedClass,
                            MethodName = ExpectedFunction
                        }
            };
#endif
    }
}
