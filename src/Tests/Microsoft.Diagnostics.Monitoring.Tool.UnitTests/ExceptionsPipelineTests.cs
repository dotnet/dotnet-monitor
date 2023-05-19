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
                    TestExceptionsStore.ExceptionInstance instance = Assert.Single(instances);
                    Assert.NotNull(instance);
                    Assert.NotEqual(0UL, instance.ExceptionId);
                    Assert.Equal(typeof(InvalidOperationException).FullName, instance.TypeName);
                    Assert.False(string.IsNullOrEmpty(instance.Message));
                    Assert.False(string.IsNullOrEmpty(instance.ThrowingMethodName));
                    Assert.True(instance.Timestamp > baselineTimestamp);
                    Assert.NotEmpty(instance.StackFrameIds); 
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
                    TestExceptionsStore.ExceptionInstance instance = Assert.Single(instances);
                    Assert.NotNull(instance);
                    Assert.NotEqual(0UL, instance.ExceptionId);
                    Assert.Equal(typeof(TaskCanceledException).FullName, instance.TypeName);
                    Assert.False(string.IsNullOrEmpty(instance.Message));
                    Assert.False(string.IsNullOrEmpty(instance.ThrowingMethodName));
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
                    TestExceptionsStore.ExceptionInstance instance = Assert.Single(instances);
                    Assert.NotNull(instance);
                    Assert.NotEqual(0UL, instance.ExceptionId);
                    Assert.Equal(typeof(ArgumentNullException).FullName, instance.TypeName);
                    Assert.False(string.IsNullOrEmpty(instance.Message));
                    Assert.False(string.IsNullOrEmpty(instance.ThrowingMethodName));
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
                    TestExceptionsStore.ExceptionInstance instance = Assert.Single(instances);
                    Assert.NotNull(instance);
                    Assert.NotEqual(0UL, instance.ExceptionId);
                    Assert.Equal("Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios.ExceptionsScenario+CustomGenericsException`2[System.Int32,System.String]", instance.TypeName);
                    Assert.False(string.IsNullOrEmpty(instance.Message));
                    Assert.False(string.IsNullOrEmpty(instance.ThrowingMethodName));
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
                    TestExceptionsStore.ExceptionInstance instance = Assert.Single(instances);
                    Assert.NotNull(instance);
                    Assert.NotEqual(0UL, instance.ExceptionId);
                    Assert.Equal(typeof(InvalidOperationException).FullName, instance.TypeName);
                    Assert.False(string.IsNullOrEmpty(instance.Message));
                    Assert.False(string.IsNullOrEmpty(instance.ThrowingMethodName));
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
                    TestExceptionsStore.ExceptionInstance instance = Assert.Single(instances);
                    Assert.NotNull(instance);
                    Assert.NotEqual(0UL, instance.ExceptionId);
                    Assert.Equal("Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios.ExceptionsScenario+CustomSimpleException", instance.TypeName);
                    Assert.False(string.IsNullOrEmpty(instance.Message));
                    Assert.False(string.IsNullOrEmpty(instance.ThrowingMethodName));
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
                    TestExceptionsStore.ExceptionInstance instance = Assert.Single(instances);
                    Assert.NotNull(instance);
                    Assert.NotEqual(0UL, instance.ExceptionId);
                    Assert.Equal(typeof(IndexOutOfRangeException).FullName, instance.TypeName);
                    Assert.False(string.IsNullOrEmpty(instance.Message));
                    Assert.False(string.IsNullOrEmpty(instance.ThrowingMethodName));
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

            private readonly int _instanceThreshold;
            private readonly TaskCompletionSource _instanceThresholdSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

            private int _instanceCount;

            public Task InstanceThresholdTask => _instanceThresholdSource.Task;

            public IEnumerable<ExceptionInstance> Instances => _instances;

            public TestExceptionsStore(int instanceThreshold = 1)
            {
                _instanceThreshold = instanceThreshold;
            }

            public void AddExceptionInstance(IExceptionsNameCache cache, ulong exceptionId, string message, DateTime timestamp, ulong[] stackFrameIds)
            {
                StringBuilder typeBuilder = new();
                FunctionData throwingMethodData;
                try
                {
                    Assert.True(cache.TryGetExceptionId(exceptionId, out ulong exceptionClassId, out ulong throwingMethodId, out _));

                    NameFormatter.BuildClassName(typeBuilder, cache.NameCache, exceptionClassId);

                    Assert.True(cache.NameCache.FunctionData.TryGetValue(throwingMethodId, out throwingMethodData));
                }
                catch (Exception ex)
                {
                    _instanceThresholdSource.TrySetException(ex);

                    throw;
                }

                _instances.Add(new ExceptionInstance(exceptionId, typeBuilder.ToString(), message, throwingMethodData.Name, timestamp, stackFrameIds));
                if (++_instanceCount >= _instanceThreshold)
                {
                    _instanceThresholdSource.TrySetResult();
                }
            }

            public IReadOnlyList<IExceptionInstance> GetSnapshot()
            {
                throw new NotSupportedException();
            }

            public sealed record class ExceptionInstance(ulong ExceptionId, string TypeName, string Message, string ThrowingMethodName, DateTime Timestamp, ulong[] StackFrameIds)
            {
            }
        }
    }
}
