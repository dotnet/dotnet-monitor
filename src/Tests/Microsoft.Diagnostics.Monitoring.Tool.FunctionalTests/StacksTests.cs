// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITestOutputHelper _outputHelper;
        private readonly TemporaryDirectory _tempDirectory;

        private const string ExpectedModule = @"Microsoft.Diagnostics.Monitoring.UnitTestApp.dll";
        private const string ExpectedClass = @"Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios.StacksWorker+StacksWorkerNested`1[System.Int32]";
        private const string ExpectedFunction = @"DoWork[System.Int64]";
        private const string ExpectedCallbackFunction = @"Callback";
        private const string NativeFrame = "[NativeFrame]";
        private const string ExpectedThreadName = "TestThread";

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
                    int processId = await runner.ProcessIdTask;

                    using ResponseStreamHolder holder = await client.CaptureStacksAsync(processId, WebApi.StackFormat.PlainText);
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
                    int processId = await runner.ProcessIdTask;

                    using ResponseStreamHolder holder = await client.CaptureStacksAsync(processId, WebApi.StackFormat.Json);
                    Assert.NotNull(holder);

                    WebApi.Models.CallStackResult result = await JsonSerializer.DeserializeAsync<WebApi.Models.CallStackResult>(holder.Stream);
                    WebApi.Models.CallStackFrame[] expectedFrames = ExpectedFrames();
                    (WebApi.Models.CallStack stack, IList<WebApi.Models.CallStackFrame> actualFrames) = GetActualFrames(result, expectedFrames.First(), expectedFrames.Length);

                    Assert.NotNull(stack);
                    Assert.Equal(ExpectedThreadName, stack.ThreadName);
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
        public Task TestSpeedscopeStacks(Architecture targetArchitecture)
        {
            return ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                WebApi.DiagnosticPortConnectionMode.Listen,
                TestAppScenarios.Stacks.Name,
                appValidate: async (runner, client) =>
                {
                    int processId = await runner.ProcessIdTask;

                    using ResponseStreamHolder holder = await client.CaptureStacksAsync(processId, WebApi.StackFormat.Speedscope);
                    Assert.NotNull(holder);

                    WebApi.Models.SpeedscopeResult result = await JsonSerializer.DeserializeAsync<WebApi.Models.SpeedscopeResult>(holder.Stream);

                    int bottomIndex = result.Shared.Frames.FindIndex(f => f.Name == FormatFrame(ExpectedModule, ExpectedClass, ExpectedFunction));
                    Assert.NotEqual(-1, bottomIndex);
                    string topFrameName = FormatFrame(ExpectedModule, ExpectedClass, ExpectedCallbackFunction);
                    int topIndex = result.Shared.Frames.FindIndex(f => f.Name == topFrameName);
                    Assert.NotEqual(-1, topIndex);

                    WebApi.Models.ProfileEvent[] expectedFrames = ExpectedSpeedscopeFrames(topIndex, bottomIndex);
                    (WebApi.Models.Profile stack, IList<WebApi.Models.ProfileEvent> actualFrames) = GetActualFrames(result, topFrameName, 3);

                    Assert.NotNull(stack);
                    Assert.EndsWith(ExpectedThreadName, stack.Name);
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
                    int processId = await runner.ProcessIdTask;

                    using ResponseStreamHolder holder1 = await client.CaptureStacksAsync(processId, WebApi.StackFormat.Json);
                    Assert.NotNull(holder1);

                    WebApi.Models.CallStackResult result1 = await JsonSerializer.DeserializeAsync<WebApi.Models.CallStackResult>(holder1.Stream);

                    // Wait for the operations to synchronize, this happens asynchronously from the http request returning
                    // and may not be fast enough for this test on systems with limited resources.
                    _ = await client.PollOperationToCompletion(holder1.Response.Headers.Location);

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
                    int processId = await runner.ProcessIdTask;

                    ApiStatusCodeException ex = await Assert.ThrowsAsync<ApiStatusCodeException>(() => client.CaptureStacksAsync(processId, WebApi.StackFormat.Json));
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
                    int processId = await runner.ProcessIdTask;

                    ApiStatusCodeException ex = await Assert.ThrowsAsync<ApiStatusCodeException>(() => client.CaptureStacksAsync(processId, WebApi.StackFormat.Json));
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
                    (WebApi.Models.CallStack callstack, IList<WebApi.Models.CallStackFrame> actualFrames) = GetActualFrames(result, expectedFrames.First(), expectedFrames.Length);

                    Assert.NotNull(callstack);
                    Assert.Equal(ExpectedThreadName, callstack.ThreadName);
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

        private static bool AreFramesEqual(WebApi.Models.ProfileEvent left, WebApi.Models.ProfileEvent right) =>
            (left.Frame == right.Frame) && (left.At == right.At) && (left.Type == right.Type);

        private static (WebApi.Models.Profile, IList<WebApi.Models.ProfileEvent>) GetActualFrames(WebApi.Models.SpeedscopeResult result, string expectedFirstFrame, int expectedFrameCount)
        {
            int matchingFrameIndex = -1;
            var actualFrames = new List<WebApi.Models.ProfileEvent>();
            for (int i = 0; i < result.Shared.Frames.Count; i++)
            {
                WebApi.Models.SharedFrame frame = result.Shared.Frames[i];
                if (frame.Name == expectedFirstFrame)
                {
                    matchingFrameIndex = i;
                }
            }

            foreach (WebApi.Models.Profile callstack in result.Profiles)
            {
                actualFrames.Clear();
                foreach (WebApi.Models.ProfileEvent frame in callstack.Events)
                {
                    if ((frame.Frame == matchingFrameIndex) || actualFrames.Count > 0)
                    {
                        actualFrames.Add(frame);
                        if (actualFrames.Count == expectedFrameCount)
                        {
                            return (callstack, actualFrames);
                        }
                    }
                }
            }
            return (null, actualFrames);
        }

        private static (WebApi.Models.CallStack, IList<WebApi.Models.CallStackFrame>) GetActualFrames(WebApi.Models.CallStackResult result, WebApi.Models.CallStackFrame expectedFirstFrame, int expectedFrameCount)
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
                            return (stack, actualFrames);
                        }
                    }
                }
            }
            return (null, actualFrames);
        }

        private static WebApi.Models.ProfileEvent[] ExpectedSpeedscopeFrames(int topFrameIndex, int bottomFrameIndex) => new WebApi.Models.ProfileEvent[]
        {
            new WebApi.Models.ProfileEvent
            {
                Frame = topFrameIndex,
                At = 0.0,
                Type = WebApi.Models.ProfileEventType.O
            },
            new WebApi.Models.ProfileEvent
            {
                Frame = 0,
                At = 0.0,
                Type = WebApi.Models.ProfileEventType.O
            },
            new WebApi.Models.ProfileEvent
            {
                Frame = bottomFrameIndex,
                At = 0.0,
                Type = WebApi.Models.ProfileEventType.O
            },

        };

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
    }
}
