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

        private static readonly ExceptionFilterSettings SimpleInvalidOperationException = new()
        {
            TypeName = "ExceptionsScenario",
            ExceptionType = nameof(InvalidOperationException),
            ModuleName = "UnitTestApp",
            MethodName = "ThrowAndCatchInvalidOperationException"
        };

        private static readonly ExceptionFilterSettings SimpleArgumentNullException = new()
        {
            TypeName = "ArgumentNullException",
            ExceptionType = nameof(ArgumentNullException),
            ModuleName = "CoreLib",
            MethodName = "Throw"
        };

        private static readonly ExceptionFilterSettings FullInvalidOperationException = new()
        {
            TypeName = "Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios.ExceptionsScenario",
            ExceptionType = typeof(InvalidOperationException).FullName,
            ModuleName = "Microsoft.Diagnostics.Monitoring.UnitTestApp.dll",
            MethodName = "ThrowAndCatchInvalidOperationException"
        };

        private static readonly ExceptionFilterSettings FullArgumentNullException = new()
        {
            TypeName = "System.ArgumentNullException",
            ExceptionType = typeof(ArgumentNullException).FullName,
            ModuleName = "System.Private.CoreLib.dll",
            MethodName = "Throw"
        };

        private Func<ExceptionsConfigurationSettings, IExceptionInstance, bool> IncludeFunc = (configuration, instance) => configuration.ShouldInclude(instance);
        private Func<ExceptionsConfigurationSettings, IExceptionInstance, bool> ExcludeFunc = (configuration, instance) => configuration.ShouldExclude(instance);

        private const string CustomGenericsException = "Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios.ExceptionsScenario+CustomGenericsException`2[System.Int32,System.String]";

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

                    ValidateStack(instance, "ThrowAndCatchInvalidOperationException", "Microsoft.Diagnostics.Monitoring.UnitTestApp.dll", "Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios.ExceptionsScenario", new List<string>() { "System.Boolean", "System.Boolean" }, new List<string>() { "Boolean", "Boolean" });
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

                    ValidateStack(instance1, "ThrowAndCatchInvalidOperationException", "Microsoft.Diagnostics.Monitoring.UnitTestApp.dll", "Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios.ExceptionsScenario", new List<string>() { "System.Boolean", "System.Boolean" }, new List<string>() { "Boolean", "Boolean" });
                    ValidateStack(instance2, "ThrowAndCatchInvalidOperationException", "Microsoft.Diagnostics.Monitoring.UnitTestApp.dll", "Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios.ExceptionsScenario", new List<string>() { "System.Boolean", "System.Boolean" }, new List<string>() { "Boolean", "Boolean" });
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

                    ValidateStack(instance, "ThrowForNonSuccess", "System.Private.CoreLib.dll", "System.Runtime.CompilerServices.TaskAwaiter", new List<string>() { "System.Threading.Tasks.Task" }, new List<string>() { "Task" });
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

                    ValidateStack(instance, "Throw", "System.Private.CoreLib.dll", "System.ArgumentNullException", new List<string>() { "System.String" }, new List<string>() { "String" });
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

                    ValidateStack(instance, "ThrowAndCatchEsotericStackFrameTypes", "Microsoft.Diagnostics.Monitoring.UnitTestApp.dll", "Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios.ExceptionsScenario", new List<string>() { "System.Int32&", "System.Int32&" }, new List<string>() { "Int32&", "Int32&" });
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

                    ValidateStack(instance, "ThrowAndCatchInvalidOperationException", "Microsoft.Diagnostics.Monitoring.UnitTestApp.dll", "Microsoft.Diagnostics.Monitoring.UnitTestApp.Scenarios.ExceptionsScenario", new List<string>() { "System.Boolean", "System.Boolean" }, new List<string>() { "Boolean", "Boolean" });
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
                    Assert.Null(innerInstance.CallStack); // Indicates this exception was not thrown

                    IExceptionInstance outerInstance = instances.Skip(1).Single();
                    Assert.NotNull(outerInstance);

                    Assert.NotEqual(0UL, outerInstance.Id);
                    Assert.Equal(typeof(InvalidOperationException).FullName, outerInstance.TypeName);
                    Assert.Equal(innerInstance.Id, Assert.Single(outerInstance.InnerExceptionIds));
                    Assert.NotNull(outerInstance.CallStack); // Indicates this exception was thrown
                    Assert.NotEmpty(outerInstance.CallStack.Frames);
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
                    Assert.NotNull(innerInstance.CallStack); // Indicates this exception was thrown
                    Assert.NotEmpty(innerInstance.CallStack.Frames);

                    IExceptionInstance outerInstance = instances.Skip(1).Single();
                    Assert.NotNull(outerInstance);

                    Assert.NotEqual(0UL, outerInstance.Id);
                    Assert.Equal(typeof(InvalidOperationException).FullName, outerInstance.TypeName);
                    Assert.Equal(innerInstance.Id, Assert.Single(outerInstance.InnerExceptionIds));
                    Assert.NotNull(outerInstance.CallStack); // Indicates this exception was thrown
                    Assert.NotEmpty(outerInstance.CallStack.Frames);
                });
        }

        /// <summary>
        /// Tests that wrapped exceptions thrown within a catch block are detectable
        /// (and that the outer exception's callstack is present).
        /// </summary>
        [Fact]
        public Task EventExceptionsPipeline_EclipsingException()
        {
            const int ExpectedInstanceCount = 2;

            return Execute(
                TestAppScenarios.Exceptions.SubScenarios.EclipsingException,
                ExpectedInstanceCount,
                validate: instances => ValidateEclipsingException(ExpectedInstanceCount, instances));
        }

        /// <summary>
        /// Tests that wrapped exceptions thrown from a method called within the catch block are detectable
        /// (and that the outer exception's callstack is present).
        /// </summary>
        [Fact]
        public Task EventExceptionsPipeline_EclipsingExceptionFromMethodCall()
        {
            const int ExpectedInstanceCount = 2;

            return Execute(
                TestAppScenarios.Exceptions.SubScenarios.EclipsingExceptionFromMethodCall,
                ExpectedInstanceCount,
                validate: instances => ValidateEclipsingException(ExpectedInstanceCount, instances));
        }

        private static void ValidateEclipsingException(int expectedInstanceCount, IEnumerable<IExceptionInstance> instances)
        {
            Assert.Equal(expectedInstanceCount, instances.Count());
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
            Assert.NotEmpty(outerInstance.CallStack.Frames); // Indicates this exception was thrown
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
                    Assert.Null(inner1Instance.CallStack); // Indicates this exception was not thrown

                    IExceptionInstance inner2Instance = instanceList[1];
                    Assert.NotNull(inner2Instance);

                    Assert.NotEqual(0UL, inner2Instance.Id);
                    Assert.Equal(typeof(FormatException).FullName, inner2Instance.TypeName);
                    Assert.Empty(inner2Instance.InnerExceptionIds);
                    Assert.Null(inner2Instance.CallStack); // Indicates this exception was not thrown

                    IExceptionInstance inner3Instance = instanceList[2];
                    Assert.NotNull(inner3Instance);

                    Assert.NotEqual(0UL, inner3Instance.Id);
                    Assert.Equal(typeof(TaskCanceledException).FullName, inner3Instance.TypeName);
                    Assert.Empty(inner3Instance.InnerExceptionIds);
                    Assert.Null(inner3Instance.CallStack); // Indicates this exception was not thrown

                    IExceptionInstance outerInstance = instanceList[3];
                    Assert.NotNull(outerInstance);

                    Assert.NotEqual(0UL, outerInstance.Id);
                    Assert.Equal(typeof(AggregateException).FullName, outerInstance.TypeName);
                    Assert.NotEmpty(outerInstance.InnerExceptionIds);
                    Assert.NotNull(outerInstance.CallStack); // Indicates this exception was thrown
                    Assert.NotEmpty(outerInstance.CallStack.Frames);

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
                    Assert.Null(inner1Instance.CallStack); // Indicates this exception was not thrown

                    IExceptionInstance inner2Instance = instanceList[1];
                    Assert.NotNull(inner2Instance);

                    Assert.NotEqual(0UL, inner2Instance.Id);
                    Assert.Equal(typeof(MissingFieldException).FullName, inner2Instance.TypeName);
                    Assert.Empty(inner2Instance.InnerExceptionIds);
                    Assert.Null(inner2Instance.CallStack); // Indicates this exception was not thrown

                    IExceptionInstance outerInstance = instanceList[2];
                    Assert.NotNull(outerInstance);

                    Assert.NotEqual(0UL, outerInstance.Id);
                    Assert.Equal(typeof(ReflectionTypeLoadException).FullName, outerInstance.TypeName);
                    Assert.NotEmpty(outerInstance.InnerExceptionIds);
                    Assert.NotNull(outerInstance.CallStack); // Indicates this exception was thrown
                    Assert.NotEmpty(outerInstance.CallStack.Frames);

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

                    ExceptionsConfigurationSettings full = new()
                    {
                        Include = new() { FullInvalidOperationException }
                    };
                    Assert.True(full.ShouldInclude(instance), $"Incorrectly filtered exception: {GetExceptionDetails(instance)}");

                    ExceptionsConfigurationSettings simple = new()
                    {
                        Include = new() { SimpleInvalidOperationException }
                    };
                    Assert.False(simple.ShouldInclude(instance), $"Incorrectly filtered exception: {GetExceptionDetails(instance)}");
                });
        }

        /// <summary>
        /// Tests the Include filter when multiple ExceptionConfigurations are provided.
        /// </summary>
        [Fact]
        public Task EventExceptionsPipeline_IncludeMultiple()
        {
            return Execute(
                TestAppScenarios.Exceptions.SubScenarios.MultipleExceptions,
                expectedInstanceCount: 3,
                validate: instances =>
                {
                    var expectedIncludeInstancesList = instances.Where(instance => !instance.TypeName.Equals(CustomGenericsException)).ToList();
                    var expectedNotIncludeInstancesList = instances.Where(instance => instance.TypeName.Equals(CustomGenericsException)).ToList();

                    ExceptionsConfigurationSettings full = new()
                    {
                        Include = new() { FullInvalidOperationException, FullArgumentNullException }
                    };
                    ValidateFilter(full, true, expectedIncludeInstancesList, IncludeFunc);
                    ValidateFilter(full, false, expectedNotIncludeInstancesList, IncludeFunc);

                    ExceptionsConfigurationSettings simple = new()
                    {
                        Include = new() { SimpleInvalidOperationException, SimpleArgumentNullException }
                    };
                    ValidateFilter(simple, false, instances, IncludeFunc);
                });
        }

        /// <summary>
        /// Tests the Exclude filter when multiple ExceptionConfigurations are provided.
        /// </summary>
        [Fact]
        public Task EventExceptionsPipeline_ExcludeMultiple()
        {
            return Execute(
                TestAppScenarios.Exceptions.SubScenarios.MultipleExceptions,
                expectedInstanceCount: 3,
                validate: instances =>
                {
                    var expectedExcludeInstancesList = instances.Where(instance => !instance.TypeName.Equals(CustomGenericsException)).ToList();
                    var expectedNotExcludeInstancesList = instances.Where(instance => instance.TypeName.Equals(CustomGenericsException)).ToList();

                    ExceptionsConfigurationSettings full = new()
                    {
                        Exclude = new() { FullInvalidOperationException, FullArgumentNullException }
                    };
                    ValidateFilter(full, true, expectedExcludeInstancesList, ExcludeFunc);
                    ValidateFilter(full, false, expectedNotExcludeInstancesList, ExcludeFunc);

                    ExceptionsConfigurationSettings simple = new()
                    {
                        Exclude = new() { SimpleInvalidOperationException, SimpleArgumentNullException }
                    };
                    ValidateFilter(simple, false, instances, ExcludeFunc);
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

                    ExceptionsConfigurationSettings full = new()
                    {
                        Exclude = new() { FullInvalidOperationException }
                    };
                    Assert.True(full.ShouldExclude(instance), $"Incorrectly filtered exception: {GetExceptionDetails(instance)}");

                    ExceptionsConfigurationSettings simple = new()
                    {
                        Exclude = new() { SimpleInvalidOperationException }
                    };
                    Assert.False(simple.ShouldExclude(instance), $"Incorrectly filtered exception: {GetExceptionDetails(instance)}");
                });
        }

        private static void ValidateFilter(ExceptionsConfigurationSettings configuration, bool expectedResult, IEnumerable<IExceptionInstance> instances, Func<ExceptionsConfigurationSettings, IExceptionInstance, bool> shouldFilter)
        {
            foreach (var instance in instances)
            {
                if (expectedResult)
                {
                    Assert.True(shouldFilter(configuration, instance), $"Incorrectly filtered exception: {GetExceptionDetails(instance)}");
                }
                else
                {
                    Assert.False(shouldFilter(configuration, instance), $"Incorrectly filtered exception: {GetExceptionDetails(instance)}");
                }
            }
        }

        private static string GetExceptionDetails(IExceptionInstance instance)
        {
            var topFrame = instance.CallStack?.Frames.FirstOrDefault();

            if (topFrame != null)
            {
                return $"MethodName: {topFrame.MethodName}, ModuleName: {topFrame.ModuleName}, TypeName: {topFrame.TypeName}, TypeName: {instance.TypeName}";
            }
            else
            {
                return $"TypeName: {instance.TypeName}";
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

        private static void ValidateStack(IExceptionInstance instance, string expectedMethodName, string expectedModuleName, string expectedTypeName, IList<string> expectedFullParameterTypes = null, IList<string> expectedParameterTypes = null)
        {
            CallStack stack = instance.CallStack;
            Assert.NotEmpty(stack.Frames);
            Assert.True(0 < stack.ThreadId);
            Assert.Equal(expectedMethodName, stack.Frames[0].MethodName);
            Assert.Equal(expectedModuleName, stack.Frames[0].ModuleName);
            Assert.Equal(expectedTypeName, stack.Frames[0].TypeName);
            Assert.Equal(expectedFullParameterTypes ?? new List<string>(), stack.Frames[0].FullParameterTypes);
            Assert.Equal(expectedParameterTypes ?? new List<string>(), stack.Frames[0].SimpleParameterTypes);
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

                    NameFormatter.BuildTypeName(typeBuilder, cache.NameCache, exceptionClassId, NameFormatter.TypeFormat.Full);

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
