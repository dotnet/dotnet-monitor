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
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public sealed class ExceptionsPipelineTests
    {
        private ITestOutputHelper _outputHelper;
        private readonly EndpointUtilities _endpointUtilities;

        private const ulong ExpectedFunctionId = 2;

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
            const ulong ExpectedOffset = 7;

            return Execute(
                TestAppScenarios.Exceptions.SubScenarios.SingleException,
                expectedInstanceCount: 1,
                validate: instances =>
                {
                    TestExceptionsStore.ExceptionInstance instance = Assert.Single(instances);
                    Assert.NotNull(instance);
                    Assert.NotEqual(0UL, instance.ExceptionId);
                    Assert.Equal(typeof(InvalidOperationException).FullName, instance.TypeName);
                    Assert.False(string.IsNullOrEmpty(instance.Message));
                    Assert.False(string.IsNullOrEmpty(instance.ThrowingMethodName));
                    Assert.True(instance.Timestamp > baselineTimestamp);

                    ValidateStack(instance, ExpectedFunctionId, ExpectedOffset);
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
            const ulong ExpectedOffset = 7;

            DateTime baselineTimestamp = DateTime.UtcNow;

            return Execute(
                TestAppScenarios.Exceptions.SubScenarios.RepeatException,
                expectedInstanceCount: ExpectedInstanceCount,
                validate: instances =>
                {
                    Assert.Equal(ExpectedInstanceCount, instances.Count());

                    TestExceptionsStore.ExceptionInstance instance1 = instances.First();
                    Assert.NotNull(instance1);

                    TestExceptionsStore.ExceptionInstance instance2 = instances.Skip(1).Single();
                    Assert.NotNull(instance2);

                    Assert.Equal(instance1.ExceptionId, instance2.ExceptionId);
                    Assert.Equal(instance1.TypeName, instance2.TypeName);
                    Assert.Equal(instance1.Message, instance2.Message);
                    Assert.Equal(instance1.ThrowingMethodName, instance2.ThrowingMethodName);
                    Assert.True(instance1.Timestamp > baselineTimestamp);
                    Assert.True(instance2.Timestamp > baselineTimestamp);
                    Assert.NotEqual(instance1.Timestamp, instance2.Timestamp);

                    ValidateStack(instance1, ExpectedFunctionId, ExpectedOffset);
                    ValidateStack(instance2, ExpectedFunctionId, ExpectedOffset);
                });
        }

        /// <summary>
        /// Tests that exceptions thrown from async context are only reported once.
        /// </summary>
        [Fact]
        public Task EventExceptionsPipeline_AsyncException()
        {
            const ulong ExpectedOffset = 38;

            return Execute(
                TestAppScenarios.Exceptions.SubScenarios.AsyncException,
                expectedInstanceCount: 1,
                validate: instances =>
                {
                    TestExceptionsStore.ExceptionInstance instance = Assert.Single(instances);
                    Assert.NotNull(instance);
                    Assert.NotEqual(0UL, instance.ExceptionId);
                    Assert.Equal(typeof(TaskCanceledException).FullName, instance.TypeName);
                    Assert.False(string.IsNullOrEmpty(instance.Message));
                    Assert.False(string.IsNullOrEmpty(instance.ThrowingMethodName));

                    ValidateStack(instance, ExpectedFunctionId, ExpectedOffset);
                });
        }

        /// <summary>
        /// Tests that exceptions thrown from framework code (non-user code) are detectable.
        /// </summary>
        [Fact]
        public Task EventExceptionsPipeline_FrameworkException()
        {
            const ulong ExpectedOffset = 6;

            return Execute(
                TestAppScenarios.Exceptions.SubScenarios.FrameworkException,
                expectedInstanceCount: 1,
                validate: instances =>
                {
                    TestExceptionsStore.ExceptionInstance instance = Assert.Single(instances);
                    Assert.NotNull(instance);
                    Assert.NotEqual(0UL, instance.ExceptionId);
                    Assert.Equal(typeof(ArgumentNullException).FullName, instance.TypeName);
                    Assert.False(string.IsNullOrEmpty(instance.Message));
                    Assert.False(string.IsNullOrEmpty(instance.ThrowingMethodName));

                    ValidateStack(instance, ExpectedFunctionId, ExpectedOffset);
                });
        }

        /// <summary>
        /// Tests that custom exceptions are detectable.
        /// </summary>
        [Fact]
        public Task EventExceptionsPipeline_CustomException()
        {
            const ulong ExpectedOffset = 12;

            return Execute(
                TestAppScenarios.Exceptions.SubScenarios.CustomException,
                expectedInstanceCount: 1,
                validate: instances =>
                {
                    TestExceptionsStore.ExceptionInstance instance = Assert.Single(instances);
                    Assert.NotNull(instance);
                    Assert.NotEqual(0UL, instance.ExceptionId);
                    Assert.Equal("Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios.ExceptionsScenario+CustomGenericsException`2[System.Int32,System.String]", instance.TypeName);
                    Assert.False(string.IsNullOrEmpty(instance.Message));
                    Assert.False(string.IsNullOrEmpty(instance.ThrowingMethodName));

                    ValidateStack(instance, ExpectedFunctionId, ExpectedOffset);
                });
        }

        /// <summary>
        /// Tests that exceptions from reverse p/invoke are detectable.
        /// </summary>
        [Theory]
        [MemberData(nameof(ProfilerHelper.GetArchitecture), MemberType = typeof(ProfilerHelper))]
        public Task EventExceptionsPipeline_ReversePInvokeException(Architecture architecture)
        {
            const ulong ExpectedOffset = 7;

            return Execute(
                TestAppScenarios.Exceptions.SubScenarios.ReversePInvokeException,
                expectedInstanceCount: 1,
                validate: instances =>
                {
                    TestExceptionsStore.ExceptionInstance instance = Assert.Single(instances);
                    Assert.NotNull(instance);
                    Assert.NotEqual(0UL, instance.ExceptionId);
                    Assert.Equal(typeof(InvalidOperationException).FullName, instance.TypeName);
                    Assert.False(string.IsNullOrEmpty(instance.Message));
                    Assert.False(string.IsNullOrEmpty(instance.ThrowingMethodName));

                    ValidateStack(instance, ExpectedFunctionId, ExpectedOffset);
                },
                architecture);
        }

        /// <summary>
        /// Tests that exceptions from dynamic methods are detectable.
        /// </summary>
        [Fact]
        public Task EventExceptionsPipeline_DynamicMethodException()
        {
            const ulong ExpectedOffset = 18446744073709551615;

            return Execute(
                TestAppScenarios.Exceptions.SubScenarios.DynamicMethodException,
                expectedInstanceCount: 1,
                validate: instances =>
                {
                    TestExceptionsStore.ExceptionInstance instance = Assert.Single(instances);
                    Assert.NotNull(instance);
                    Assert.NotEqual(0UL, instance.ExceptionId);
                    Assert.Equal("Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios.ExceptionsScenario+CustomSimpleException", instance.TypeName);
                    Assert.False(string.IsNullOrEmpty(instance.Message));
                    Assert.False(string.IsNullOrEmpty(instance.ThrowingMethodName));

                    ValidateStack(instance, ExpectedFunctionId, ExpectedOffset);
                });
        }

        /// <summary>
        /// Tests that exceptions from array types are detectable.
        /// </summary>
        [Fact]
        public Task EventExceptionsPipeline_ArrayException()
        {
            const ulong ExpectedOffset = 5;

            return Execute(
                TestAppScenarios.Exceptions.SubScenarios.ArrayException,
                expectedInstanceCount: 1,
                validate: instances =>
                {
                    TestExceptionsStore.ExceptionInstance instance = Assert.Single(instances);
                    Assert.NotNull(instance);
                    Assert.NotEqual(0UL, instance.ExceptionId);
                    Assert.Equal(typeof(IndexOutOfRangeException).FullName, instance.TypeName);
                    Assert.False(string.IsNullOrEmpty(instance.Message));
                    Assert.False(string.IsNullOrEmpty(instance.ThrowingMethodName));

                    ValidateStack(instance, ExpectedFunctionId, ExpectedOffset);
                });
        }

        private async Task Execute(
            string subScenarioName,
            int expectedInstanceCount,
            Action<IEnumerable<TestExceptionsStore.ExceptionInstance>> validate,
            Architecture? architecture = null)
        {
            EndpointInfoSourceCallback callback = new(_outputHelper);
            await using ServerSourceHolder sourceHolder = await _endpointUtilities.StartServerAsync(callback);

            await using AppRunner runner = _endpointUtilities.CreateAppRunner(
                Assembly.GetExecutingAssembly(),
                sourceHolder.TransportName,
                TargetFrameworkMoniker.Current);
            runner.Architecture = architecture;
            runner.ScenarioName = TestAppScenarios.Exceptions.Name + " " + subScenarioName;

            AddStartupHookEnvironmentVariable(runner);

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

                validate(store.Instances);
            });
        }

        private static void ValidateStack(TestExceptionsStore.ExceptionInstance instance, ulong expectedFunctionId, ulong expectedOffset)
        {
            CallStack stack = Assert.Single(instance.CallStackResult.Stacks);
            Assert.NotEmpty(stack.Frames);
            Assert.Equal(expectedFunctionId, stack.Frames[0].FunctionId);
            Assert.Equal(expectedOffset, stack.Frames[0].Offset);
        }

        private static void AddStartupHookEnvironmentVariable(AppRunner runner)
        {
            // Startup hook assembly is only built for net6.0 and should be forward compatible
            string startupHookPath = AssemblyHelper.GetAssemblyArtifactBinPath(
                Assembly.GetExecutingAssembly(),
                "Microsoft.Diagnostics.Monitoring.StartupHook",
                TargetFrameworkMoniker.Net60);

            runner.Environment.Add(ToolIdentifiers.EnvironmentVariables.StartupHooks, startupHookPath);
        }

        // check that we get the correct top frame
        private sealed class TestExceptionsStore : IExceptionsStore
        {
            private readonly List<ExceptionInstance> _instances = new();
            private readonly List<IExceptionsNameCache> _caches = new();

            private readonly int _instanceThreshold;
            private readonly TaskCompletionSource _instanceThresholdSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

            private int _instanceCount;

            public Task InstanceThresholdTask => _instanceThresholdSource.Task;

            public IEnumerable<ExceptionInstance> Instances => _instances;
            public IEnumerable<IExceptionsNameCache> Caches => _caches;

            public TestExceptionsStore(int instanceThreshold = 1)
            {
                _instanceThreshold = instanceThreshold;
            }

            public void AddExceptionInstance(IExceptionsNameCache cache, ulong exceptionId, string message, DateTime timestamp, ulong[] stackFrameIds)
            {
                StringBuilder typeBuilder = new();
                FunctionData throwingMethodData;
                CallStackResult result;
                try
                {
                    Assert.True(cache.TryGetExceptionId(exceptionId, out ulong exceptionClassId, out ulong throwingMethodId, out _));

                    NameFormatter.BuildClassName(typeBuilder, cache.NameCache, exceptionClassId);

                    Assert.True(cache.NameCache.FunctionData.TryGetValue(throwingMethodId, out throwingMethodData));

                    result = ExceptionsStore.GenerateCallStackResult(stackFrameIds, cache);
                }
                catch (Exception ex)
                {
                    _instanceThresholdSource.TrySetException(ex);

                    throw;
                }

                _instances.Add(new ExceptionInstance(exceptionId, typeBuilder.ToString(), message, throwingMethodData.Name, timestamp, result));
                _caches.Add(cache);
                if (++_instanceCount >= _instanceThreshold)
                {
                    _instanceThresholdSource.TrySetResult();
                }
            }

            public IReadOnlyList<IExceptionInstance> GetSnapshot()
            {
                throw new NotSupportedException();
            }

            public sealed record class ExceptionInstance(ulong ExceptionId, string TypeName, string Message, string ThrowingMethodName, DateTime Timestamp, CallStackResult CallStackResult)
            {
            }
        }
    }
}
