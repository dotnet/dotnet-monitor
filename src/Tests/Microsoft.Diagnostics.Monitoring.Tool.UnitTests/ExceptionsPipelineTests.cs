// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Exceptions;
using Microsoft.Diagnostics.Monitoring.WebApi.Stacks;
using Microsoft.Diagnostics.Tools.Monitor.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using CallStack = Microsoft.Diagnostics.Monitoring.WebApi.Models.CallStack;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public sealed class ExceptionsPipelineTests
    {
        private ITestOutputHelper _outputHelper;
        private readonly EndpointUtilities _endpointUtilities;

        // Startup hook assembly is only built for net6.0 and should be forward compatible
        private static string StartupHookPath => AssemblyHelper.GetAssemblyArtifactBinPath(
            Assembly.GetExecutingAssembly(),
            "Microsoft.Diagnostics.Monitoring.StartupHook",
            TargetFrameworkMoniker.Net60);

        private static ExceptionConfiguration simpleInvalidOperationException = new()
        {
            ClassName = "ExceptionsScenario",
            ExceptionType = nameof(InvalidOperationException),
            ModuleName = "UnitTestApp",
            MethodName = "ThrowAndCatchInvalidOperationException"
        };

        private static ExceptionConfiguration simpleArgumentNullException = new()
        {
            ClassName = "ArgumentNullException",
            ExceptionType = nameof(ArgumentNullException),
            ModuleName = "CoreLib",
            MethodName = "Throw"
        };

        private static ExceptionConfiguration fullInvalidOperationException = new()
        {
            ClassName = "Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios.ExceptionsScenario",
            ExceptionType = typeof(InvalidOperationException).FullName,
            ModuleName = "Microsoft.Diagnostics.Monitoring.UnitTestApp.dll",
            MethodName = "ThrowAndCatchInvalidOperationException"
        };

        private static ExceptionConfiguration fullArgumentNullException = new()
        {
            ClassName = "System.ArgumentNullException",
            ExceptionType = typeof(ArgumentNullException).FullName,
            ModuleName = "System.Private.CoreLib.dll",
            MethodName = "Throw"
        };

        private Func<ExceptionsConfiguration, IExceptionInstance, bool> includeFunc = (configuration, instance) => configuration.ShouldInclude(instance);
        private Func<ExceptionsConfiguration, IExceptionInstance, bool> excludeFunc = (configuration, instance) => configuration.ShouldExclude(instance);

        private const string CustomGenericsException = "CustomGenericsException";

        public ExceptionsPipelineTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
            _endpointUtilities = new(_outputHelper);
        }

        /// <summary>
        /// Tests that a single exception thrown from the target application is detectable.
        /// </summary>
        [Fact]
        public Task EventExceptionsPipeline_SingleException()
        {
            DateTime baselineTimestamp = DateTime.UtcNow;

            return Execute(
                TestAppScenarios.Exceptions.SubScenarios.SingleException,
                expectedInstanceCount: 1,
                validate: instances =>
                {
                    IExceptionInstance instance = Assert.Single(instances);
                    Assert.NotNull(instance);
                    Assert.NotEqual(0UL, instance.Id);
                    Assert.Equal(typeof(InvalidOperationException).FullName, instance.TypeName);
                    Assert.False(string.IsNullOrEmpty(instance.Message));
                    Assert.True(instance.Timestamp > baselineTimestamp);

                    ValidateStack(instance, "ThrowAndCatchInvalidOperationException", "Microsoft.Diagnostics.Monitoring.UnitTestApp.dll", "Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios.ExceptionsScenario", new List<string>() { "System.Boolean", "System.Boolean" });
                });
        }

        /// <summary>
        /// Tests that the same exception type thrown from the same code is reported as the same exception
        /// but as two distinct instances.
        /// </summary>
        [Fact]
        public Task EventExceptionsPipeline_RepeatException()
        {
            const int ExpectedInstanceCount = 2;

            DateTime baselineTimestamp = DateTime.UtcNow;

            return Execute(
                TestAppScenarios.Exceptions.SubScenarios.RepeatException,
                expectedInstanceCount: ExpectedInstanceCount,
                validate: instances =>
                {
                    Assert.Equal(ExpectedInstanceCount, instances.Count());

                    IExceptionInstance instance1 = instances.First();
                    Assert.NotNull(instance1);

                    IExceptionInstance instance2 = instances.Skip(1).Single();
                    Assert.NotNull(instance2);

                    Assert.NotEqual(instance1.Id, instance2.Id);
                    Assert.Equal(instance1.TypeName, instance2.TypeName);
                    Assert.Equal(instance1.Message, instance2.Message);
                    Assert.True(instance1.Timestamp > baselineTimestamp);
                    Assert.True(instance2.Timestamp > baselineTimestamp);
                    Assert.NotEqual(instance1.Timestamp, instance2.Timestamp);

                    ValidateStack(instance1, "ThrowAndCatchInvalidOperationException", "Microsoft.Diagnostics.Monitoring.UnitTestApp.dll", "Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios.ExceptionsScenario", new List<string>() { "System.Boolean", "System.Boolean" });
                    ValidateStack(instance2, "ThrowAndCatchInvalidOperationException", "Microsoft.Diagnostics.Monitoring.UnitTestApp.dll", "Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios.ExceptionsScenario", new List<string>() { "System.Boolean", "System.Boolean" });
                });
        }

        /// <summary>
        /// Tests that exceptions thrown from async context are only reported once.
        /// </summary>
        [Fact]
        public Task EventExceptionsPipeline_AsyncException()
        {
            return Execute(
                TestAppScenarios.Exceptions.SubScenarios.AsyncException,
                expectedInstanceCount: 1,
                validate: instances =>
                {
                    IExceptionInstance instance = Assert.Single(instances);
                    Assert.NotNull(instance);
                    Assert.NotEqual(0UL, instance.Id);
                    Assert.Equal(typeof(TaskCanceledException).FullName, instance.TypeName);
                    Assert.False(string.IsNullOrEmpty(instance.Message));

                    ValidateStack(instance, "ThrowForNonSuccess", "System.Private.CoreLib.dll", "System.Runtime.CompilerServices.TaskAwaiter", new List<string>() { "System.Threading.Tasks.Task" });
                });
        }

        /// <summary>
        /// Tests that exceptions thrown from framework code (non-user code) are detectable.
        /// </summary>
        [Fact]
        public Task EventExceptionsPipeline_FrameworkException()
        {
            return Execute(
                TestAppScenarios.Exceptions.SubScenarios.FrameworkException,
                expectedInstanceCount: 1,
                validate: instances =>
                {
                    IExceptionInstance instance = Assert.Single(instances);
                    Assert.NotNull(instance);
                    Assert.NotEqual(0UL, instance.Id);
                    Assert.Equal(typeof(ArgumentNullException).FullName, instance.TypeName);
                    Assert.False(string.IsNullOrEmpty(instance.Message));

                    ValidateStack(instance, "Throw", "System.Private.CoreLib.dll", "System.ArgumentNullException", new List<string>() { "System.String" });
                });
        }

        /// <summary>
        /// Tests that custom exceptions are detectable.
        /// </summary>
        [Fact]
        public Task EventExceptionsPipeline_CustomException()
        {
            return Execute(
                TestAppScenarios.Exceptions.SubScenarios.CustomException,
                expectedInstanceCount: 1,
                validate: instances =>
                {
                    IExceptionInstance instance = Assert.Single(instances);
                    Assert.NotNull(instance);
                    Assert.NotEqual(0UL, instance.Id);
                    Assert.Equal("Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios.ExceptionsScenario+CustomGenericsException`2[System.Int32,System.String]", instance.TypeName);
                    Assert.False(string.IsNullOrEmpty(instance.Message));

                    ValidateStack(instance, "ThrowAndCatchCustomException", "Microsoft.Diagnostics.Monitoring.UnitTestApp.dll", "Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios.ExceptionsScenario");
                });
        }

        /// <summary>
        /// Tests that exceptions with ref parameters and void* parameters are handled correctly.
        /// </summary>
        [Fact]
        public Task EventExceptionsPipeline_EsotericStackFrameTypes()
        {
            return Execute(
                TestAppScenarios.Exceptions.SubScenarios.EsotericStackFrameTypes,
                expectedInstanceCount: 1,
                validate: instances =>
                {
                    IExceptionInstance instance = Assert.Single(instances);
                    Assert.NotNull(instance);
                    Assert.NotEqual(0UL, instance.Id);
                    Assert.Equal(typeof(FormatException).FullName, instance.TypeName);
                    Assert.False(string.IsNullOrEmpty(instance.Message));

                    ValidateStack(instance, "ThrowAndCatchEsotericStackFrameTypes", "Microsoft.Diagnostics.Monitoring.UnitTestApp.dll", "Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios.ExceptionsScenario", new List<string>() { "System.Int32&", "System.Int32&" });
                });
        }

        /// <summary>
        /// Tests that exceptions from reverse p/invoke are detectable.
        /// </summary>
        [Theory]
        [MemberData(nameof(ProfilerHelper.GetArchitecture), MemberType = typeof(ProfilerHelper))]
        public Task EventExceptionsPipeline_ReversePInvokeException(Architecture architecture)
        {
            return Execute(
                TestAppScenarios.Exceptions.SubScenarios.ReversePInvokeException,
                expectedInstanceCount: 1,
                validate: instances =>
                {
                    IExceptionInstance instance = Assert.Single(instances);
                    Assert.NotNull(instance);
                    Assert.NotEqual(0UL, instance.Id);
                    Assert.Equal(typeof(InvalidOperationException).FullName, instance.TypeName);
                    Assert.False(string.IsNullOrEmpty(instance.Message));

                    ValidateStack(instance, "ThrowAndCatchInvalidOperationException", "Microsoft.Diagnostics.Monitoring.UnitTestApp.dll", "Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios.ExceptionsScenario", new List<string>() { "System.Boolean", "System.Boolean" });
                },
                architecture);
        }

        /// <summary>
        /// Tests that exceptions from dynamic methods are detectable.
        /// </summary>
        [Fact]
        public Task EventExceptionsPipeline_DynamicMethodException()
        {
            return Execute(
                TestAppScenarios.Exceptions.SubScenarios.DynamicMethodException,
                expectedInstanceCount: 1,
                validate: instances =>
                {
                    IExceptionInstance instance = Assert.Single(instances);
                    Assert.NotNull(instance);
                    Assert.NotEqual(0UL, instance.Id);
                    Assert.Equal("Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios.ExceptionsScenario+CustomSimpleException", instance.TypeName);
                    Assert.False(string.IsNullOrEmpty(instance.Message));

                    ValidateStack(instance, "ThrowAndCatchFromDynamicMethod", "Microsoft.Diagnostics.Monitoring.UnitTestApp.dll", "UnknownClass");
                });
        }

        /// <summary>
        /// Tests that exceptions from array types are detectable.
        /// </summary>
        [Fact]
        public Task EventExceptionsPipeline_ArrayException()
        {
            return Execute(
                TestAppScenarios.Exceptions.SubScenarios.ArrayException,
                expectedInstanceCount: 1,
                validate: instances =>
                {
                    IExceptionInstance instance = Assert.Single(instances);
                    Assert.NotNull(instance);
                    Assert.NotEqual(0UL, instance.Id);
                    Assert.Equal(typeof(IndexOutOfRangeException).FullName, instance.TypeName);
                    Assert.False(string.IsNullOrEmpty(instance.Message));

                    ValidateStack(instance, "ThrowIndexOutOfRangeException", "System.Private.CoreLib.dll", "System.ThrowHelper");
                });
        }

        /// <summary>
        /// Tests that exceptions with inner exceptions that haven't been thrown are detectable.
        /// </summary>
        [Fact]
        public Task EventExceptionsPipeline_InnerUnthrownException()
        {
            const int ExpectedInstanceCount = 2;

            return Execute(
                TestAppScenarios.Exceptions.SubScenarios.InnerUnthrownException,
                ExpectedInstanceCount,
                validate: instances =>
                {
                    Assert.Equal(ExpectedInstanceCount, instances.Count());

                    IExceptionInstance innerInstance = instances.First();
                    Assert.NotNull(innerInstance);

                    Assert.NotEqual(0UL, innerInstance.Id);
                    Assert.Equal(typeof(FormatException).FullName, innerInstance.TypeName);
                    Assert.Empty(innerInstance.InnerExceptionIds);
                    Assert.Empty(innerInstance.CallStack.Frames); // Indicates this exception was not thrown

                    IExceptionInstance outerInstance = instances.Skip(1).Single();
                    Assert.NotNull(outerInstance);

                    Assert.NotEqual(0UL, outerInstance.Id);
                    Assert.Equal(typeof(InvalidOperationException).FullName, outerInstance.TypeName);
                    Assert.Equal(innerInstance.Id, Assert.Single(outerInstance.InnerExceptionIds));
                    Assert.NotEmpty(outerInstance.CallStack.Frames); // Indicates this exception was thrown
                });
        }

        /// <summary>
        /// Tests that exceptions with inner exceptions that haven't been thrown are detectable.
        /// </summary>
        [Fact]
        public Task EventExceptionsPipeline_InnerThrownException()
        {
            const int ExpectedInstanceCount = 2;

            return Execute(
                TestAppScenarios.Exceptions.SubScenarios.InnerThrownException,
                ExpectedInstanceCount,
                validate: instances =>
                {
                    Assert.Equal(ExpectedInstanceCount, instances.Count());

                    IExceptionInstance innerInstance = instances.First();
                    Assert.NotNull(innerInstance);

                    Assert.NotEqual(0UL, innerInstance.Id);
                    Assert.Equal(typeof(FormatException).FullName, innerInstance.TypeName);
                    Assert.Empty(innerInstance.InnerExceptionIds);
                    Assert.NotEmpty(innerInstance.CallStack.Frames); // Indicates this exception was thrown

                    IExceptionInstance outerInstance = instances.Skip(1).Single();
                    Assert.NotNull(outerInstance);

                    Assert.NotEqual(0UL, outerInstance.Id);
                    Assert.Equal(typeof(InvalidOperationException).FullName, outerInstance.TypeName);
                    Assert.Equal(innerInstance.Id, Assert.Single(outerInstance.InnerExceptionIds));
                    Assert.NotEmpty(innerInstance.CallStack.Frames); // Indicates this exception was thrown
                });
        }

        /// <summary>
        /// Tests that inner exceptions from AggregateException are detectable.
        /// </summary>
        [Fact]
        public Task EventExceptionsPipeline_AggregateException()
        {
            const int ExpectedInstanceCount = 4;

            return Execute(
                TestAppScenarios.Exceptions.SubScenarios.AggregateException,
                ExpectedInstanceCount,
                validate: instances =>
                {
                    List<IExceptionInstance> instanceList = new(instances);

                    Assert.Equal(ExpectedInstanceCount, instances.Count());

                    IExceptionInstance inner1Instance = instanceList[0];
                    Assert.NotNull(inner1Instance);

                    Assert.NotEqual(0UL, inner1Instance.Id);
                    Assert.Equal(typeof(InvalidOperationException).FullName, inner1Instance.TypeName);
                    Assert.Empty(inner1Instance.InnerExceptionIds);
                    Assert.Empty(inner1Instance.CallStack.Frames); // Indicates this exception was not thrown

                    IExceptionInstance inner2Instance = instanceList[1];
                    Assert.NotNull(inner2Instance);

                    Assert.NotEqual(0UL, inner2Instance.Id);
                    Assert.Equal(typeof(FormatException).FullName, inner2Instance.TypeName);
                    Assert.Empty(inner2Instance.InnerExceptionIds);
                    Assert.Empty(inner2Instance.CallStack.Frames); // Indicates this exception was not thrown

                    IExceptionInstance inner3Instance = instanceList[2];
                    Assert.NotNull(inner3Instance);

                    Assert.NotEqual(0UL, inner3Instance.Id);
                    Assert.Equal(typeof(TaskCanceledException).FullName, inner3Instance.TypeName);
                    Assert.Empty(inner3Instance.InnerExceptionIds);
                    Assert.Empty(inner3Instance.CallStack.Frames); // Indicates this exception was not thrown

                    IExceptionInstance outerInstance = instanceList[3];
                    Assert.NotNull(outerInstance);

                    Assert.NotEqual(0UL, outerInstance.Id);
                    Assert.Equal(typeof(AggregateException).FullName, outerInstance.TypeName);
                    Assert.NotEmpty(outerInstance.InnerExceptionIds);
                    Assert.NotEmpty(outerInstance.CallStack.Frames); // Indicates this exception was thrown

                    // Verify inner exceptions of AggregateException instance
                    Assert.Equal(3, outerInstance.InnerExceptionIds.Length);
                    Assert.Equal(inner1Instance.Id, outerInstance.InnerExceptionIds[0]);
                    Assert.Equal(inner2Instance.Id, outerInstance.InnerExceptionIds[1]);
                    Assert.Equal(inner3Instance.Id, outerInstance.InnerExceptionIds[2]);
                });
        }

        /// <summary>
        /// Tests that loader exceptions from ReflectionTypeLoadException are detectable.
        /// </summary>
        [Fact]
        public Task EventExceptionsPipeline_ReflectionTypeLoadException()
        {
            const int ExpectedInstanceCount = 3;

            return Execute(
                TestAppScenarios.Exceptions.SubScenarios.ReflectionTypeLoadException,
                ExpectedInstanceCount,
                validate: instances =>
                {
                    List<IExceptionInstance> instanceList = new(instances);

                    Assert.Equal(ExpectedInstanceCount, instanceList.Count);

                    IExceptionInstance inner1Instance = instanceList[0];
                    Assert.NotNull(inner1Instance);

                    Assert.NotEqual(0UL, inner1Instance.Id);
                    Assert.Equal(typeof(MissingMethodException).FullName, inner1Instance.TypeName);
                    Assert.Empty(inner1Instance.InnerExceptionIds);
                    Assert.Empty(inner1Instance.CallStack.Frames); // Indicates this exception was not thrown

                    IExceptionInstance inner2Instance = instanceList[1];
                    Assert.NotNull(inner2Instance);

                    Assert.NotEqual(0UL, inner2Instance.Id);
                    Assert.Equal(typeof(MissingFieldException).FullName, inner2Instance.TypeName);
                    Assert.Empty(inner2Instance.InnerExceptionIds);
                    Assert.Empty(inner2Instance.CallStack.Frames); // Indicates this exception was not thrown

                    IExceptionInstance outerInstance = instanceList[2];
                    Assert.NotNull(outerInstance);

                    Assert.NotEqual(0UL, outerInstance.Id);
                    Assert.Equal(typeof(ReflectionTypeLoadException).FullName, outerInstance.TypeName);
                    Assert.NotEmpty(outerInstance.InnerExceptionIds);
                    Assert.NotEmpty(outerInstance.CallStack.Frames); // Indicates this exception was thrown

                    // Verify inner exceptions of ReflectionTypeLoadException instance
                    Assert.Equal(3, outerInstance.InnerExceptionIds.Length);
                    Assert.Equal(inner1Instance.Id, outerInstance.InnerExceptionIds[0]);
                    Assert.Equal(0UL, outerInstance.InnerExceptionIds[1]); // Second loaded exception is null
                    Assert.Equal(inner2Instance.Id, outerInstance.InnerExceptionIds[2]);
                });
        }

        /// <summary>
        /// Tests the Include filter when a single ExceptionConfiguration is provided.
        /// </summary>
        [Fact]
        public Task EventExceptionsPipeline_IncludeSingle()
        {
            return Execute(
                TestAppScenarios.Exceptions.SubScenarios.SingleException,
                expectedInstanceCount: 1,
                validate: instances =>
                {
                    IExceptionInstance instance = Assert.Single(instances);

                    ExceptionsConfiguration full = new ExceptionsConfiguration()
                    {
                        AllowSimplifiedNames = false,
                        Include = new() { fullInvalidOperationException }
                    };
                    Assert.True(full.ShouldInclude(instance));

                    ExceptionsConfiguration simpleDisallowSimplifiedNames = new ExceptionsConfiguration()
                    {
                        AllowSimplifiedNames = false,
                        Include = new() { simpleInvalidOperationException }
                    };
                    Assert.False(simpleDisallowSimplifiedNames.ShouldInclude(instance));

                    ExceptionsConfiguration simpleAllowSimplifiedNames = new ExceptionsConfiguration()
                    {
                        AllowSimplifiedNames = true,
                        Include = new() { simpleInvalidOperationException }
                    };
                    Assert.True(simpleAllowSimplifiedNames.ShouldInclude(instance));
                });
        }

        /// <summary>
        /// Tests the Include filter when multiple ExceptionConfigurations are provided.
        /// </summary>
        [Fact]
        public Task EventExceptionsPipeline_IncludeMultiple()
        {
            return Execute(
                TestAppScenarios.Exceptions.SubScenarios.FilteringExceptions,
                expectedInstanceCount: 3,
                validate: instances =>
                {
                    var expectedIncludeInstancesList = instances.Where(instance => !instance.TypeName.Contains(CustomGenericsException)).ToList();
                    var expectedNotIncludeInstancesList = instances.Where(instance => instance.TypeName.Contains(CustomGenericsException)).ToList();

                    ExceptionsConfiguration full = new ExceptionsConfiguration()
                    {
                        AllowSimplifiedNames = false,
                        Include = new() { fullInvalidOperationException, fullArgumentNullException }
                    };
                    ValidateFilter(full, true, expectedIncludeInstancesList, includeFunc);
                    ValidateFilter(full, false, expectedNotIncludeInstancesList, includeFunc);

                    ExceptionsConfiguration simpleDisallowSimplifiedNames = new ExceptionsConfiguration()
                    {
                        AllowSimplifiedNames = false,
                        Include = new() { simpleInvalidOperationException, simpleArgumentNullException }
                    };
                    ValidateFilter(simpleDisallowSimplifiedNames, false, instances, includeFunc);

                    ExceptionsConfiguration simpleAllowSimplifiedNames = new ExceptionsConfiguration()
                    {
                        AllowSimplifiedNames = true,
                        Include = new() { simpleInvalidOperationException, simpleArgumentNullException }
                    };

                    ValidateFilter(simpleAllowSimplifiedNames, true, expectedIncludeInstancesList, includeFunc);
                    ValidateFilter(simpleAllowSimplifiedNames, false, expectedNotIncludeInstancesList, includeFunc);
                });
        }

        /// <summary>
        /// Tests the Exclude filter when multiple ExceptionConfigurations are provided.
        /// </summary>
        [Fact]
        public Task EventExceptionsPipeline_ExcludeMultiple()
        {
            return Execute(
                TestAppScenarios.Exceptions.SubScenarios.FilteringExceptions,
                expectedInstanceCount: 3,
                validate: instances =>
                {
                    var expectedExcludeInstancesList = instances.Where(instance => !instance.TypeName.Contains(CustomGenericsException)).ToList();
                    var expectedNotExcludeInstancesList = instances.Where(instance => instance.TypeName.Contains(CustomGenericsException)).ToList();

                    ExceptionsConfiguration full = new ExceptionsConfiguration()
                    {
                        AllowSimplifiedNames = false,
                        Exclude = new() { fullInvalidOperationException, fullArgumentNullException }
                    };
                    ValidateFilter(full, true, expectedExcludeInstancesList, excludeFunc);
                    ValidateFilter(full, false, expectedNotExcludeInstancesList, excludeFunc);

                    ExceptionsConfiguration simpleDisallowSimplifiedNames = new ExceptionsConfiguration()
                    {
                        AllowSimplifiedNames = false,
                        Exclude = new() { simpleInvalidOperationException, simpleArgumentNullException }
                    };
                    ValidateFilter(simpleDisallowSimplifiedNames, false, instances, excludeFunc);

                    ExceptionsConfiguration simpleAllowSimplifiedNames = new ExceptionsConfiguration()
                    {
                        AllowSimplifiedNames = true,
                        Exclude = new() { simpleInvalidOperationException, simpleArgumentNullException }
                    };
                    ValidateFilter(simpleAllowSimplifiedNames, true, expectedExcludeInstancesList, excludeFunc);
                    ValidateFilter(simpleAllowSimplifiedNames, false, expectedNotExcludeInstancesList, excludeFunc);
                });
        }

        /// <summary>
        /// Tests the Exclude filter when a single ExceptionConfiguration is provided.
        /// </summary>
        [Fact]
        public Task EventExceptionsPipeline_ExcludeBasic()
        {
            return Execute(
                TestAppScenarios.Exceptions.SubScenarios.SingleException,
                expectedInstanceCount: 1,
                validate: instances =>
                {
                    IExceptionInstance instance = Assert.Single(instances);

                    ExceptionsConfiguration full = new ExceptionsConfiguration()
                    {
                        AllowSimplifiedNames = false,
                        Exclude = new() { fullInvalidOperationException }
                    };
                    Assert.True(full.ShouldExclude(instance));

                    ExceptionsConfiguration simpleDisallowSimplifiedNames = new ExceptionsConfiguration()
                    {
                        AllowSimplifiedNames = false,
                        Exclude = new() { simpleInvalidOperationException }
                    };
                    Assert.False(simpleDisallowSimplifiedNames.ShouldExclude(instance));

                    ExceptionsConfiguration simpleAllowSimplifiedNames = new ExceptionsConfiguration()
                    {
                        AllowSimplifiedNames = true,
                        Exclude = new() { simpleInvalidOperationException }
                    };
                    Assert.True(simpleAllowSimplifiedNames.ShouldExclude(instance));
                });
        }

        private static void ValidateFilter(ExceptionsConfiguration configuration, bool expectedResult, IEnumerable<IExceptionInstance> instances, Func<ExceptionsConfiguration, IExceptionInstance, bool> shouldFilter)
        {
            foreach (var instance in instances)
            {
                Assert.Equal(expectedResult, shouldFilter(configuration, instance));
            }
        }

        private async Task Execute(
            string subScenarioName,
            int expectedInstanceCount,
            Action<IEnumerable<IExceptionInstance>> validate,
            Architecture? architecture = null)
        {
            string startupHookPathForCallback = null;
#if NET8_0_OR_GREATER
            // Starting in .NET 8, the startup hook can be applied dynamically via DiagnosticsClient.
            startupHookPathForCallback = StartupHookPath;
#endif
            EndpointInfoSourceCallback callback = new(_outputHelper, startupHookPathForCallback);
            await using ServerSourceHolder sourceHolder = await _endpointUtilities.StartServerAsync(callback);

            await using AppRunner runner = _endpointUtilities.CreateAppRunner(
                Assembly.GetExecutingAssembly(),
                sourceHolder.TransportName,
                TargetFrameworkMoniker.Current);
            runner.Architecture = architecture;
            runner.ScenarioName = TestAppScenarios.Exceptions.Name + " " + subScenarioName;

#if !NET8_0_OR_GREATER
            // Runtimes lower than .NET 8 will require setting the startup hook environment variable explicitly.
            runner.Environment.Add(ToolIdentifiers.EnvironmentVariables.StartupHooks, StartupHookPath);
#endif

            Task<IEndpointInfo> newEndpointInfoTask = callback.WaitAddedEndpointInfoAsync(runner, CommonTestTimeouts.StartProcess);

            await runner.ExecuteAsync(async () =>
            {
                await newEndpointInfoTask;

                TestExceptionsStore store = new(expectedInstanceCount);

                EventExceptionsPipelineSettings settings = new();
                await using EventExceptionsPipeline pipeline = new(newEndpointInfoTask.Result.Endpoint, settings, store);

                using CancellationTokenSource timeoutSource = new(CommonTestTimeouts.GeneralTimeout);
                await pipeline.StartAsync(timeoutSource.Token);

                // Start throwing exceptions.
                await runner.SendCommandAsync(TestAppScenarios.Exceptions.Commands.Begin);

                // The target process will not acknowledge this command until all exceptions are thrown.
                await runner.SendCommandAsync(TestAppScenarios.Exceptions.Commands.End);

                // Wait for the expected number of exceptions to have been reported
                await store.InstanceThresholdTask.WaitAsync(timeoutSource.Token);

                validate(store.GetSnapshot());
            });
        }

        private static void ValidateStack(IExceptionInstance instance, string expectedMethodName, string expectedModuleName, string expectedClassName, IList<string> expectedParameterTypes = null)
        {
            CallStack stack = instance.CallStack;
            Assert.NotEmpty(stack.Frames);
            Assert.True(0 < stack.ThreadId);
            Assert.Equal(expectedMethodName, stack.Frames[0].MethodName);
            Assert.Equal(expectedModuleName, stack.Frames[0].ModuleName);
            Assert.Equal(expectedClassName, stack.Frames[0].ClassName);
            Assert.Equal(expectedParameterTypes ?? new List<string>(), stack.Frames[0].ParameterTypes);
        }

        private sealed class TestExceptionsStore : IExceptionsStore
        {
            private readonly List<ExceptionInstance> _instances = new();

            private readonly int _instanceThreshold;
            private readonly TaskCompletionSource _instanceThresholdSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

            private int _instanceCount;

            public Task InstanceThresholdTask => _instanceThresholdSource.Task;

            public TestExceptionsStore(int instanceThreshold = 1)
            {
                _instanceThreshold = instanceThreshold;
            }

            public void AddExceptionInstance(IExceptionsNameCache cache, ulong exceptionId, ulong groupId, string message, DateTime timestamp, ulong[] stackFrameIds, int threadId, ulong[] innerExceptionIds, string activityId, ActivityIdFormat activityIdFormat)
            {
                string moduleName = string.Empty;
                StringBuilder typeBuilder = new();
                CallStack callStack;
                try
                {
                    Assert.True(cache.TryGetExceptionGroup(groupId, out ulong exceptionClassId, out _, out _));

                    NameFormatter.BuildClassName(typeBuilder, cache.NameCache, exceptionClassId);

                    if (cache.NameCache.ClassData.TryGetValue(exceptionClassId, out ClassData exceptionClassData))
                    {
                        moduleName = NameFormatter.GetModuleName(cache.NameCache, exceptionClassData.ModuleId);
                    }

                    callStack = ExceptionsStore.GenerateCallStack(stackFrameIds, cache, threadId);
                }
                catch (Exception ex)
                {
                    _instanceThresholdSource.TrySetException(ex);

                    throw;
                }

                _instances.Add(new ExceptionInstance(
                    exceptionId,
                    moduleName,
                    typeBuilder.ToString(),
                    message,
                    timestamp,
                    callStack,
                    innerExceptionIds,
                    activityId,
                    activityIdFormat));

                if (++_instanceCount >= _instanceThreshold)
                {
                    _instanceThresholdSource.TrySetResult();
                }
            }

            public void RemoveExceptionInstance(ulong exceptionId)
            {
                throw new NotSupportedException();
            }

            public IReadOnlyList<IExceptionInstance> GetSnapshot()
            {
                return _instances;
            }

            public sealed record class ExceptionInstance(ulong Id, string ModuleName, string TypeName, string Message, DateTime Timestamp, CallStack CallStack, ulong[] InnerExceptionIds, string ActivityId, ActivityIdFormat ActivityIdFormat)
                : IExceptionInstance
            {
            }
        }
    }
}
