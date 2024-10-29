// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Fixtures;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.HttpApi;
using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Runners;
using Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using ProcessInfo = Microsoft.Diagnostics.Monitoring.WebApi.Models.ProcessInfo;

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
        private const string ExpectedTextFunction = @"DoWork[Int64]";
        private const string ExpectedCallbackFunction = @"Callback";
        private const string NativeFrame = "[NativeFrame]";
        private const string ExpectedThreadName = "TestThread";

        private static MethodInfo GetMethodInfo(string typeName, string methodName)
        {
            static void removeGenericInformation(ref string name)
            {
                if (name.Contains('['))
                {
                    name = name[..name.IndexOf('[')];
                }
            }

            // Strip off any generic type information.
            removeGenericInformation(ref typeName);
            removeGenericInformation(ref methodName);

            // Return null on pseudo frames (e.g. [NativeFrame])
            if (methodName.Length == 0)
            {
                return null;
            }

            Type typeMatch = typeof(StacksWorker).Module.GetType(typeName);
            Assert.NotNull(typeMatch);

            return typeMatch.GetMethod(methodName);
        }

        public StacksTests(ITestOutputHelper outputHelper, ServiceProviderFixture serviceProviderFixture)
        {
            _httpClientFactory = serviceProviderFixture.ServiceProvider.GetService<IHttpClientFactory>();
            _outputHelper = outputHelper;
            _tempDirectory = new TemporaryDirectory(_outputHelper);
        }

        [Theory]
        [MemberData(nameof(ProfilerHelper.GetArchitecture), MemberType = typeof(ProfilerHelper))]
        public Task TestPlainTextStacksListenSuspend(Architecture targetArchitecture)
        {
            return TestStacksListenSuspend(targetArchitecture, PlainTextValidation);
        }

        [Theory]
        [MemberData(nameof(ProfilerHelper.GetArchitecture), MemberType = typeof(ProfilerHelper))]
        public Task TestPlainTextStacksListenNoSuspend(Architecture targetArchitecture)
        {
            return TestStacksListenNoSuspend(targetArchitecture, PlainTextValidation);
        }

        private static async Task PlainTextValidation(AppRunner runner, ApiClient client)
        {
            int processId = await runner.ProcessIdTask;

            using ResponseStreamHolder holder = await client.CaptureStacksAsync(processId, WebApi.StackFormat.PlainText);
            Assert.NotNull(holder);

            using StreamReader reader = new StreamReader(holder.Stream);
            string line = null;

            string[] expectedFrames =
            {
                FormatFrame(ExpectedModule, typeof(HiddenFrameTestMethods).FullName, nameof(HiddenFrameTestMethods.ExitPoint)),
                FormatFrame(ExpectedModule, typeof(HiddenFrameTestMethods.PartiallyVisibleClass).FullName, nameof(HiddenFrameTestMethods.PartiallyVisibleClass.DoWorkFromVisibleDerivedClass)),
                FormatFrame(ExpectedModule, typeof(HiddenFrameTestMethods).FullName, nameof(HiddenFrameTestMethods.EntryPoint)),
                FormatFrame(ExpectedModule, ExpectedClass, ExpectedCallbackFunction),
                NativeFrame,
                FormatFrame(ExpectedModule, ExpectedClass, ExpectedTextFunction),
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
        }

        [Theory]
        [MemberData(nameof(ProfilerHelper.GetArchitecture), MemberType = typeof(ProfilerHelper))]
        public Task TestJsonStacksListenSuspend(Architecture targetArchitecture)
        {
            return TestStacksListenSuspend(
                targetArchitecture,
                validate: (runner, client) => JsonValidation(runner, client, isSuspendedAtStartup: true));
        }

        [Theory]
        [MemberData(nameof(ProfilerHelper.GetArchitecture), MemberType = typeof(ProfilerHelper))]
        public Task TestJsonStacksListenNoSuspend(Architecture targetArchitecture)
        {
            return TestStacksListenNoSuspend(
                targetArchitecture,
                validate: (runner, client) => JsonValidation(runner, client, isSuspendedAtStartup: false));
        }

        private static async Task JsonValidation(AppRunner runner, ApiClient client, bool isSuspendedAtStartup)
        {
            int processId = await runner.ProcessIdTask;

            using ResponseStreamHolder holder = await client.CaptureStacksAsync(processId, WebApi.StackFormat.Json);
            Assert.NotNull(holder);

            WebApi.Models.CallStackResult result = await JsonSerializer.DeserializeAsync<WebApi.Models.CallStackResult>(holder.Stream);
            WebApi.Models.CallStackFrame[] expectedFrames = ExpectedFrames();
            (WebApi.Models.CallStack stack, IList<WebApi.Models.CallStackFrame> actualFrames) = GetActualFrames(result, expectedFrames.First(), expectedFrames.Length);

            Assert.NotNull(stack);

            if (isSuspendedAtStartup)
            {
                // If process connects to .NET Monitor with suspend (the default),
                // then the profiler is loaded at startup and thread names are tracked.
                Assert.Equal(ExpectedThreadName, stack.ThreadName);
            }

            Assert.Equal(expectedFrames.Length, actualFrames.Count);
            for (int i = 0; i < expectedFrames.Length; i++)
            {
                Assert.True(AreFramesEqual(expectedFrames[i], actualFrames[i]));
            }
        }

        [Theory]
        [MemberData(nameof(ProfilerHelper.GetArchitecture), MemberType = typeof(ProfilerHelper))]
        public Task TestSpeedscopeStacksListenSuspend(Architecture targetArchitecture)
        {
            return TestStacksListenSuspend(
                targetArchitecture,
                (runner, client) => SpeedscopeStacksValidation(runner, client, isSuspendedAtStartup: true));
        }

        [Theory]
        [MemberData(nameof(ProfilerHelper.GetArchitecture), MemberType = typeof(ProfilerHelper))]
        public Task TestSpeedscopeStacksListenNoSuspend(Architecture targetArchitecture)
        {
            return TestStacksListenNoSuspend(
                targetArchitecture,
                (runner, client) => SpeedscopeStacksValidation(runner, client, isSuspendedAtStartup: false));
        }

        private static async Task SpeedscopeStacksValidation(AppRunner runner, ApiClient client, bool isSuspendedAtStartup)
        {
            int processId = await runner.ProcessIdTask;

            using ResponseStreamHolder holder = await client.CaptureStacksAsync(processId, WebApi.StackFormat.Speedscope);
            Assert.NotNull(holder);

            WebApi.Models.SpeedscopeResult result = await JsonSerializer.DeserializeAsync<WebApi.Models.SpeedscopeResult>(holder.Stream);

            string[] framesToFind =
            [
                FormatFrame(ExpectedModule, typeof(HiddenFrameTestMethods).FullName, nameof(HiddenFrameTestMethods.ExitPoint)),
                FormatFrame(ExpectedModule, typeof(HiddenFrameTestMethods.PartiallyVisibleClass).FullName, nameof(HiddenFrameTestMethods.PartiallyVisibleClass.DoWorkFromVisibleDerivedClass)),
                FormatFrame(ExpectedModule, typeof(HiddenFrameTestMethods).FullName, nameof(HiddenFrameTestMethods.EntryPoint)),
                FormatFrame(ExpectedModule, ExpectedClass, ExpectedCallbackFunction),
                NativeFrame,
                FormatFrame(ExpectedModule, ExpectedClass, ExpectedFunction)
            ];

            int[] indices = framesToFind.Select(frame => result.Shared.Frames.FindIndex(f => f.Name == frame)).ToArray();
            Assert.DoesNotContain(-1, indices);

            WebApi.Models.ProfileEvent[] expectedFrames = ExpectedSpeedscopeFrames(indices);
            (WebApi.Models.Profile stack, IList<WebApi.Models.ProfileEvent> actualFrames) = GetActualFrames(result, framesToFind[0], framesToFind.Length);

            Assert.NotNull(stack);

            if (isSuspendedAtStartup)
            {
                // If process connects to .NET Monitor with suspend (the default),
                // then the profiler is loaded at startup and thread names are tracked.
                Assert.EndsWith(ExpectedThreadName, stack.Name);
            }

            Assert.Equal(expectedFrames.Length, actualFrames.Count);
            for (int i = 0; i < expectedFrames.Length; i++)
            {
                Assert.True(AreFramesEqual(expectedFrames[i], actualFrames[i]));
            }
        }

        [Theory]
        [MemberData(nameof(ProfilerHelper.GetArchitecture), MemberType = typeof(ProfilerHelper))]
        public Task TestRepeatStackCallsListenSuspend(Architecture targetArchitecture)
        {
            return TestStacksListenSuspend(targetArchitecture, RepeatStackCallsValidation);
        }

        [Theory]
        [MemberData(nameof(ProfilerHelper.GetArchitecture), MemberType = typeof(ProfilerHelper))]
        public Task TestRepeatStackCallsListenNoSuspend(Architecture targetArchitecture)
        {
            return TestStacksListenNoSuspend(targetArchitecture, RepeatStackCallsValidation);
        }

        private static async Task RepeatStackCallsValidation(AppRunner runner, ApiClient client)
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
        }

        /// <summary>
        /// Verifies that the /stacks route returns 404 if the stacks feature is disabled.
        /// </summary>
        [Theory]
        [MemberData(nameof(ProfilerHelper.GetArchitecture), MemberType = typeof(ProfilerHelper))]
        public Task TestInProcessFeaturesEnabledCallStacksDisabled(Architecture targetArchitecture)
        {
            return ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                WebApi.DiagnosticPortConnectionMode.Listen,
                TestAppScenarios.Stacks.Name,
                appValidate: async (runner, client) =>
                {
                    int processId = await runner.ProcessIdTask;

                    ValidationProblemDetailsException ex = await Assert.ThrowsAsync<ValidationProblemDetailsException>(() => client.CaptureStacksAsync(processId, WebApi.StackFormat.Json));
                    Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);

                    await runner.SendCommandAsync(TestAppScenarios.Stacks.Commands.Continue);
                },
                configureApp: runner =>
                {
                    runner.Architecture = targetArchitecture;
                },
                configureTool: runner =>
                {
                    runner.ConfigurationFromEnvironment.EnableInProcessFeatures();
                    runner.ConfigurationFromEnvironment.DisableCallStacks();
                });
        }

        /// <summary>
        /// Verifies that the /stacks route returns 404 if the in-process features are disabled.
        /// </summary>
        [Theory]
        [MemberData(nameof(ProfilerHelper.GetArchitecture), MemberType = typeof(ProfilerHelper))]
        public Task TestInProcessFeaturesDisabledCallStacksEnabled(Architecture targetArchitecture)
        {
            return ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                WebApi.DiagnosticPortConnectionMode.Listen,
                TestAppScenarios.Stacks.Name,
                appValidate: async (runner, client) =>
                {
                    int processId = await runner.ProcessIdTask;

                    ValidationProblemDetailsException ex = await Assert.ThrowsAsync<ValidationProblemDetailsException>(() => client.CaptureStacksAsync(processId, WebApi.StackFormat.Json));
                    Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);

                    await runner.SendCommandAsync(TestAppScenarios.Stacks.Commands.Continue);
                },
                configureApp: runner =>
                {
                    runner.Architecture = targetArchitecture;
                },
                configureTool: runner =>
                {
                    runner.ConfigurationFromEnvironment.DisableInProcessFeatures();
                    runner.ConfigurationFromEnvironment.EnableCallStacks();
                });
        }

        [Theory(Skip = "Disable unstable tests.")]
        [MemberData(nameof(ProfilerHelper.GetArchitecture), MemberType = typeof(ProfilerHelper))]
        public Task TestCollectStacksActionListenSuspend(Architecture targetArchitecture)
        {
            Task ruleCompletedTask = null;

            return TestStacksListenSuspend(
                targetArchitecture,
                (runner, client) => CollectStacksActionValidation(runner, client, ruleCompletedTask, isSuspendedAtStartup: true),
                runner => CollectStacksActionConfigureTool(runner, out ruleCompletedTask));
        }

        [Theory(Skip = "Disable unstable tests.")]
        [MemberData(nameof(ProfilerHelper.GetArchitecture), MemberType = typeof(ProfilerHelper))]
        public Task TestCollectStacksActionListenNoSuspend(Architecture targetArchitecture)
        {
            Task ruleCompletedTask = null;

            return TestStacksListenNoSuspend(
                targetArchitecture,
                (runner, client) => CollectStacksActionValidation(runner, client, ruleCompletedTask, isSuspendedAtStartup: false),
                runner => CollectStacksActionConfigureTool(runner, out ruleCompletedTask));
        }

        private async Task CollectStacksActionValidation(AppRunner runner, ApiClient client, Task ruleCompletedTask, bool isSuspendedAtStartup)
        {
            await ruleCompletedTask;

            string[] files = Directory.GetFiles(_tempDirectory.FullName, "*");
            Assert.Single(files);
            using FileStream stream = File.OpenRead(files.First());

            WebApi.Models.CallStackResult result = await JsonSerializer.DeserializeAsync<WebApi.Models.CallStackResult>(stream);
            WebApi.Models.CallStackFrame[] expectedFrames = ExpectedFrames();
            (WebApi.Models.CallStack callstack, IList<WebApi.Models.CallStackFrame> actualFrames) = GetActualFrames(result, expectedFrames.First(), expectedFrames.Length);

            Assert.NotNull(callstack);

            if (isSuspendedAtStartup)
            {
                // If process connects to .NET Monitor with suspend (the default),
                // then the profiler is loaded at startup and thread names are tracked.
                Assert.Equal(ExpectedThreadName, callstack.ThreadName);
            }

            Assert.Equal(expectedFrames.Length, actualFrames.Count);
            for (int i = 0; i < expectedFrames.Length; i++)
            {
                Assert.True(AreFramesEqual(expectedFrames[i], actualFrames[i]));
            }
        }

        private void CollectStacksActionConfigureTool(MonitorCollectRunner runner, out Task ruleCompletedTask)
        {
            const string fileEgress = nameof(fileEgress);
            runner.ConfigurationFromEnvironment
                .EnableInProcessFeatures()
                .EnableCallStacks()
                .AddFileSystemEgress(fileEgress, _tempDirectory.FullName)
                .CreateCollectionRule("StacksCounterRule")
                .SetEventCounterTrigger(options =>
                {
                    options.ProviderName = "StackScenario";
                    options.CounterName = "Ready";
                    options.GreaterThan = 0.0;
                    options.SlidingWindowDuration = TimeSpan.FromSeconds(5);
                })
                .AddCollectStacksAction(fileEgress, o => o.Format = Tools.Monitor.CollectionRules.Options.Actions.CallStackFormat.Json);

            ruleCompletedTask = runner.WaitForCollectionRuleActionsCompletedAsync("StacksCounterRule");
        }

        private Task TestStacksListenSuspend(
            Architecture targetArchitecture,
            Func<AppRunner, ApiClient, Task> validate,
            Action<MonitorCollectRunner> configureTool = null)
        {
            return ScenarioRunner.SingleTarget(
                _outputHelper,
                _httpClientFactory,
                WebApi.DiagnosticPortConnectionMode.Listen,
                TestAppScenarios.Stacks.Name,
                appValidate: async (runner, client) =>
                {
                    int processId = await runner.ProcessIdTask;
                    ProcessInfo processInfo = await client.GetProcessWithRetryAsync(_outputHelper, pid: processId);

                    await ProfilerHelper.WaitForProfilerCommunicationChannelAsync(processInfo);

                    await validate.Invoke(runner, client);

                    await runner.SendCommandAsync(TestAppScenarios.Stacks.Commands.Continue);
                },
                configureApp: runner =>
                {
                    runner.Architecture = targetArchitecture;
                },
                configureTool: runner =>
                {
                    runner.ConfigurationFromEnvironment.EnableCallStacks();

                    configureTool?.Invoke(runner);
                });
        }

        private async Task TestStacksListenNoSuspend(
            Architecture targetArchitecture,
            Func<AppRunner, ApiClient, Task> validate,
            Action<MonitorCollectRunner> configureTool = null)
        {
            DiagnosticPortHelper.Generate(
                WebApi.DiagnosticPortConnectionMode.Listen,
                out _,
                out string diagnosticPortPath);

            // Startup app before .NET Monitor
            await using AppRunner appRunner = new(_outputHelper, Assembly.GetExecutingAssembly());
            appRunner.ConnectionMode = WebApi.DiagnosticPortConnectionMode.Connect;
            appRunner.DiagnosticPortPath = diagnosticPortPath;
            appRunner.DiagnosticPortSuspend = false; // nosuspend
            appRunner.ScenarioName = TestAppScenarios.Stacks.Name;
            appRunner.Architecture = targetArchitecture;

            await appRunner.ExecuteAsync(async () =>
            {
                // App is executing managed code at this point

                // Start .NET Monitor
                await using MonitorCollectRunner toolRunner = new(_outputHelper);
                toolRunner.ConnectionModeViaCommandLine = WebApi.DiagnosticPortConnectionMode.Listen;
                toolRunner.DiagnosticPortPath = diagnosticPortPath;
                toolRunner.DisableAuthentication = true;
                toolRunner.ConfigurationFromEnvironment.EnableCallStacks();

                configureTool?.Invoke(toolRunner);

                await toolRunner.StartAsync();

                using HttpClient httpClient = await toolRunner.CreateHttpClientDefaultAddressAsync(_httpClientFactory);
                ApiClient apiClient = new(_outputHelper, httpClient);

                // Wait for the process to be discovered.
                int processId = await appRunner.ProcessIdTask;
                ProcessInfo processInfo = await apiClient.GetProcessWithRetryAsync(_outputHelper, pid: processId);

                await ProfilerHelper.WaitForProfilerCommunicationChannelAsync(processInfo);

                await validate.Invoke(appRunner, apiClient);

                await appRunner.SendCommandAsync(TestAppScenarios.Stacks.Commands.Continue);
            });
        }

        private static string FormatFrame(string module, string @class, string function) =>
            FormattableString.Invariant($"{module}!{@class}.{function}");

        private static bool AreFramesEqual(WebApi.Models.CallStackFrame expected, WebApi.Models.CallStackFrame actual)
        {
            MethodInfo expectedMethodInfo = GetMethodInfo(expected.TypeName, expected.MethodName);

            return (expected.ModuleName == actual.ModuleName) &&
                (expected.TypeName == actual.TypeName) &&
                (expected.MethodName == actual.MethodName) &&
                ((expectedMethodInfo?.MetadataToken ?? 0) == actual.MethodToken) &&
                ((expectedMethodInfo?.Module.ModuleVersionId ?? Guid.Empty) == actual.ModuleVersionId);

        }

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

        private static WebApi.Models.ProfileEvent[] ExpectedSpeedscopeFrames(int[] indices)
            => indices.Select((i) => new WebApi.Models.ProfileEvent
            {
                Frame = i,
                At = 0.0,
                Type = WebApi.Models.ProfileEventType.O
            }).ToArray();

        private static WebApi.Models.CallStackFrame[] ExpectedFrames() => new WebApi.Models.CallStackFrame[]
            {
                new WebApi.Models.CallStackFrame
                {
                    ModuleName = ExpectedModule,
                    TypeName = typeof(HiddenFrameTestMethods).FullName,
                    MethodNameWithGenericArgTypes = nameof(HiddenFrameTestMethods.ExitPoint),
                },
                new WebApi.Models.CallStackFrame
                {
                    ModuleName = ExpectedModule,
                    TypeName = typeof(HiddenFrameTestMethods).FullName,
                    MethodNameWithGenericArgTypes = nameof(HiddenFrameTestMethods.DoWorkFromHiddenMethod),
                    Hidden = true,
                },
                new WebApi.Models.CallStackFrame
                {
                    ModuleName = ExpectedModule,
                    TypeName = typeof(HiddenFrameTestMethods.BaseHiddenClass).FullName,
                    MethodNameWithGenericArgTypes = nameof(HiddenFrameTestMethods.BaseHiddenClass.DoWorkFromHiddenBaseClass),
                    Hidden = true
                },
                new WebApi.Models.CallStackFrame
                {
                    ModuleName = ExpectedModule,
                    TypeName = typeof(HiddenFrameTestMethods.PartiallyVisibleClass).FullName,
                    MethodNameWithGenericArgTypes = nameof(HiddenFrameTestMethods.PartiallyVisibleClass.DoWorkFromVisibleDerivedClass),
                },
                new WebApi.Models.CallStackFrame
                {
                    ModuleName = ExpectedModule,
                    TypeName = typeof(HiddenFrameTestMethods).FullName,
                    MethodNameWithGenericArgTypes = nameof(HiddenFrameTestMethods.EntryPoint),
                },
                new WebApi.Models.CallStackFrame
                {
                    ModuleName = ExpectedModule,
                    TypeName = ExpectedClass,
                    MethodNameWithGenericArgTypes = ExpectedCallbackFunction,
                },
                new WebApi.Models.CallStackFrame
                {
                    ModuleName = NativeFrame,
                    TypeName = NativeFrame,
                    MethodNameWithGenericArgTypes = NativeFrame,
                },
                new WebApi.Models.CallStackFrame
                {
                    ModuleName = ExpectedModule,
                    TypeName = ExpectedClass,
                    MethodNameWithGenericArgTypes = ExpectedFunction,
                }
            };
    }
}
