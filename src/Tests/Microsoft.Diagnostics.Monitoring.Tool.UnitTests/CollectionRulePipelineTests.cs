// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.Tool.UnitTests.CollectionRules.Triggers;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public class CollectionRulePipelineTests
    {
        private readonly TimeSpan DefaultPipelineTimeout = TimeSpan.FromSeconds(30);
        private const string TestRuleName = "TestPipelineRule";

        private readonly ITestOutputHelper _outputHelper;

        public CollectionRulePipelineTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        /// <summary>
        /// Test that the pipeline works with the Startup trigger.
        /// </summary>
        [Theory]
        [MemberData(nameof(GetTfmsSupportingPortListener))]
        public Task CollectionRulePipeline_StartupTriggerTest(TargetFrameworkMoniker appTfm)
        {
            CallbackActionService callbackService = new(_outputHelper);

            return CollectionRulePipelineTestsHelper.ExecuteScenario(
                appTfm,
                TestAppScenarios.AsyncWait.Name,
                TestRuleName,
                options =>
                {
                    options.CreateCollectionRule(TestRuleName)
                        .SetStartupTrigger()
                        .AddAction(CallbackAction.ActionName);
                },
                async (runner, pipeline, callbacks) =>
                {
                    using CancellationTokenSource cancellationSource = new(DefaultPipelineTimeout);

                    Task startedTask = callbacks.StartWaitForPipelineStarted();

                    // Register first callback before pipeline starts. This callback should be completed before
                    // the pipeline finishes starting.
                    Task actionStarted1Task = await callbackService.StartWaitForCallbackAsync(cancellationSource.Token);

                    // Startup trigger will cause the the pipeline to complete the start phase
                    // after the action list has been completed.
                    Task runTask = pipeline.RunAsync(cancellationSource.Token);

                    await startedTask.WithCancellation(cancellationSource.Token);

                    // Register second callback after pipeline starts. The second callback should not be
                    // completed because it was registered after the pipeline had finished starting. Since
                    // the action list is only ever executed once and is executed before the pipeline finishes
                    // starting, thus subsequent invocations of the action list should not occur.
                    Task actionStarted2Task = await callbackService.StartWaitForCallbackAsync(cancellationSource.Token);

                    // Since the action list was completed before the pipeline finished starting,
                    // the action should have invoked it's callback.
                    await actionStarted1Task.WithCancellation(cancellationSource.Token);

                    // Regardless of the action list constraints, the pipeline should have only
                    // executed the action list once due to the use of a startup trigger.
                    VerifyExecutionCount(callbackService, 1);

                    // Validate that the action list was not executed a second time.
                    Assert.False(actionStarted2Task.IsCompletedSuccessfully);

                    // Pipeline should have completed shortly after finished starting. This should only
                    // wait for a very short time, if at all.
                    await runTask.WithCancellation(cancellationSource.Token);

                    // Validate that the action list was not executed a second time.
                    Assert.False(actionStarted2Task.IsCompletedSuccessfully);

                    await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                },
                _outputHelper,
                services =>
                {
                    services.RegisterTestAction(callbackService);
                });
        }

        /// <summary>
        /// Test that the pipeline works with the EventCounter trigger.
        /// </summary>
        [Theory(Skip = "Nondeterministic")]
        [MemberData(nameof(GetTfmsSupportingPortListener))]
        public Task CollectionRulePipeline_EventCounterTriggerTest(TargetFrameworkMoniker appTfm)
        {
            CallbackActionService callbackService = new(_outputHelper);

            return CollectionRulePipelineTestsHelper.ExecuteScenario(
                appTfm,
                TestAppScenarios.SpinWait.Name,
                TestRuleName,
                options =>
                {
                    options.CreateCollectionRule(TestRuleName)
                        .SetEventCounterTrigger(options =>
                        {
                            // cpu usage greater that 5% for 2 seconds
                            options.ProviderName = "System.Runtime";
                            options.CounterName = "cpu-usage";
                            options.GreaterThan = 5;
                            options.SlidingWindowDuration = TimeSpan.FromSeconds(2);
                        })
                        .AddAction(CallbackAction.ActionName);
                },
                async (runner, pipeline, callbacks) =>
                {
                    using CancellationTokenSource cancellationSource = new(DefaultPipelineTimeout);

                    Task startedTask = callbacks.StartWaitForPipelineStarted();

                    // Register first callback before pipeline starts. This callback should be completed after
                    // the pipeline finishes starting.
                    Task actionStartedTask = await callbackService.StartWaitForCallbackAsync(cancellationSource.Token);

                    // Start pipeline with EventCounter trigger.
                    Task runTask = pipeline.RunAsync(cancellationSource.Token);

                    await startedTask.WithCancellation(cancellationSource.Token);

                    await runner.SendCommandAsync(TestAppScenarios.SpinWait.Commands.StartSpin);

                    // This should not complete until the trigger conditions are satisfied for the first time.
                    await actionStartedTask.WithCancellation(cancellationSource.Token);

                    VerifyExecutionCount(callbackService, 1);

                    await runner.SendCommandAsync(TestAppScenarios.SpinWait.Commands.StopSpin);

                    // Validate that the pipeline is not in a completed state.
                    // The pipeline should already be running since it was started.
                    Assert.False(runTask.IsCompleted);

                    await pipeline.StopAsync(cancellationSource.Token);
                },
                _outputHelper,
                services =>
                {
                    services.RegisterTestAction(callbackService);
                });
        }

        /// <summary>
        /// Test that the pipeline works with the EventMeter trigger (gauge instrument).
        /// </summary>
        [Theory]
        [MemberData(nameof(GetCurrentTfm))]
        public Task CollectionRulePipeline_EventMeterTriggerTest_Gauge(TargetFrameworkMoniker appTfm)
        {
            CallbackActionService callbackService = new(_outputHelper);

            return CollectionRulePipelineTestsHelper.ExecuteScenario(
                appTfm,
                TestAppScenarios.Metrics.Name,
                TestRuleName,
                options =>
                {
                    options.GlobalCounter = new WebApi.GlobalCounterOptions()
                    {
                        IntervalSeconds = 1
                    };

                    options.CreateCollectionRule(TestRuleName)
                        .SetEventMeterTrigger(options =>
                        {
                            // gauge greater than 0 for 2 seconds
                            options.MeterName = LiveMetricsTestConstants.ProviderName1;
                            options.InstrumentName = LiveMetricsTestConstants.GaugeName;
                            options.GreaterThan = 0;
                            options.SlidingWindowDuration = TimeSpan.FromSeconds(2);
                        })
                        .AddAction(CallbackAction.ActionName);
                },
                async (runner, pipeline, callbacks) =>
                {
                    using CancellationTokenSource cancellationSource = new(DefaultPipelineTimeout);

                    Task startedTask = callbacks.StartWaitForPipelineStarted();

                    // Register first callback before pipeline starts. This callback should be completed after
                    // the pipeline finishes starting.
                    Task actionStartedTask = await callbackService.StartWaitForCallbackAsync(cancellationSource.Token);

                    // Start pipeline with EventMeter trigger.
                    Task runTask = pipeline.RunAsync(cancellationSource.Token);

                    await startedTask.WithCancellation(cancellationSource.Token);

                    // This should not complete until the trigger conditions are satisfied for the first time.
                    await actionStartedTask.WithCancellation(cancellationSource.Token);

                    VerifyExecutionCount(callbackService, 1);

                    await runner.SendCommandAsync(TestAppScenarios.Metrics.Commands.Continue);

                    // Validate that the pipeline is not in a completed state.
                    // The pipeline should already be running since it was started.
                    Assert.False(runTask.IsCompleted);

                    await pipeline.StopAsync(cancellationSource.Token);
                },
                _outputHelper,
                services =>
                {
                    services.RegisterTestAction(callbackService);
                });
        }

        /// <summary>
        /// Test that the pipeline works with the EventMeter trigger greater-than (histogram instrument).
        /// </summary>
        [Theory(Skip = "https://github.com/dotnet/dotnet-monitor/issues/6154")]
        [MemberData(nameof(GetCurrentTfm))]
        public Task CollectionRulePipeline_EventMeterTriggerTest_Histogram_GreaterThan(TargetFrameworkMoniker appTfm)
        {
            CallbackActionService callbackService = new(_outputHelper);

            return CollectionRulePipelineTestsHelper.ExecuteScenario(
                appTfm,
                TestAppScenarios.Metrics.Name,
                TestRuleName,
                options =>
                {
                    options.GlobalCounter = new WebApi.GlobalCounterOptions()
                    {
                        IntervalSeconds = 1
                    };

                    options.CreateCollectionRule(TestRuleName)
                        .SetEventMeterTrigger(options =>
                        {
                            // histogram 95th percentile greater than 60 for 2 seconds
                            options.MeterName = LiveMetricsTestConstants.ProviderName1;
                            options.InstrumentName = LiveMetricsTestConstants.HistogramName1;
                            options.HistogramPercentile = 95;
                            options.GreaterThan = 0;
                            options.SlidingWindowDuration = TimeSpan.FromSeconds(2);
                        })
                        .AddAction(CallbackAction.ActionName);
                },
                async (runner, pipeline, callbacks) =>
                {
                    using CancellationTokenSource cancellationSource = new(DefaultPipelineTimeout);

                    Task startedTask = callbacks.StartWaitForPipelineStarted();

                    // Register first callback before pipeline starts. This callback should be completed after
                    // the pipeline finishes starting.
                    Task actionStartedTask = await callbackService.StartWaitForCallbackAsync(cancellationSource.Token);

                    // Start pipeline with EventMeter trigger.
                    Task runTask = pipeline.RunAsync(cancellationSource.Token);

                    await startedTask.WithCancellation(cancellationSource.Token);

                    // This should not complete until the trigger conditions are satisfied for the first time.
                    await actionStartedTask.WithCancellation(cancellationSource.Token);

                    VerifyExecutionCount(callbackService, 1);

                    await runner.SendCommandAsync(TestAppScenarios.Metrics.Commands.Continue);

                    // Validate that the pipeline is not in a completed state.
                    // The pipeline should already be running since it was started.
                    Assert.False(runTask.IsCompleted);

                    await pipeline.StopAsync(cancellationSource.Token);
                },
                _outputHelper,
                services =>
                {
                    services.RegisterTestAction(callbackService);
                });
        }

        /// <summary>
        /// Test that the pipeline works with the EventMeter trigger less-than (histogram instrument).
        /// </summary>
        [Theory(Skip = "https://github.com/dotnet/dotnet-monitor/issues/4184")]
        [MemberData(nameof(GetCurrentTfm))]
        public Task CollectionRulePipeline_EventMeterTriggerTest_Histogram_LessThan(TargetFrameworkMoniker appTfm)
        {
            CallbackActionService callbackService = new(_outputHelper);

            return CollectionRulePipelineTestsHelper.ExecuteScenario(
                appTfm,
                TestAppScenarios.Metrics.Name,
                TestRuleName,
                options =>
                {
                    options.GlobalCounter = new WebApi.GlobalCounterOptions()
                    {
                        IntervalSeconds = 1
                    };

                    options.CreateCollectionRule(TestRuleName)
                        .SetEventMeterTrigger(options =>
                        {
                            // histogram 50% percentile less than 75 for 2 seconds
                            options.MeterName = LiveMetricsTestConstants.ProviderName1;
                            options.InstrumentName = LiveMetricsTestConstants.HistogramName1;
                            options.HistogramPercentile = 50;
                            options.LessThan = 101;
                            options.SlidingWindowDuration = TimeSpan.FromSeconds(2);
                        })
                        .AddAction(CallbackAction.ActionName);
                },
                async (runner, pipeline, callbacks) =>
                {
                    using CancellationTokenSource cancellationSource = new(DefaultPipelineTimeout);

                    Task startedTask = callbacks.StartWaitForPipelineStarted();

                    // Register first callback before pipeline starts. This callback should be completed after
                    // the pipeline finishes starting.
                    Task actionStartedTask = await callbackService.StartWaitForCallbackAsync(cancellationSource.Token);

                    // Start pipeline with EventMeter trigger.
                    Task runTask = pipeline.RunAsync(cancellationSource.Token);

                    await startedTask.WithCancellation(cancellationSource.Token);

                    // This should not complete until the trigger conditions are satisfied for the first time.
                    await actionStartedTask.WithCancellation(cancellationSource.Token);

                    VerifyExecutionCount(callbackService, 1);

                    await runner.SendCommandAsync(TestAppScenarios.Metrics.Commands.Continue);

                    // Validate that the pipeline is not in a completed state.
                    // The pipeline should already be running since it was started.
                    Assert.False(runTask.IsCompleted);

                    await pipeline.StopAsync(cancellationSource.Token);
                },
                _outputHelper,
                services =>
                {
                    services.RegisterTestAction(callbackService);
                });
        }

        /// <summary>
        /// Test that the CollectionRulePipeline completes to due to rule duration limit.
        /// </summary>
        [Theory]
        [MemberData(nameof(GetTfmsSupportingPortListener))]
        public Task CollectionRulePipeline_DurationLimitTest(TargetFrameworkMoniker appTfm)
        {
            ManualTriggerService triggerService = new();
            CallbackActionService callbackService = new(_outputHelper);

            return CollectionRulePipelineTestsHelper.ExecuteScenario(
                appTfm,
                TestAppScenarios.AsyncWait.Name,
                TestRuleName,
                options =>
                {
                    options.CreateCollectionRule(TestRuleName)
                        .SetManualTrigger()
                        .AddAction(CallbackAction.ActionName)
                        .SetDurationLimit(TimeSpan.FromSeconds(3));
                },
                async (runner, pipeline, _) =>
                {
                    using CancellationTokenSource cancellationSource = new(DefaultPipelineTimeout);

                    // Pipeline should run to completion due to rule duration limit.
                    await pipeline.RunAsync(cancellationSource.Token);

                    await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);

                    // Action list should not have been executed.
                    VerifyExecutionCount(callbackService, expectedCount: 0);
                },
                _outputHelper,
                services =>
                {
                    services.RegisterManualTrigger(triggerService);
                    services.RegisterTestAction(callbackService);
                });
        }

        /// <summary>
        /// Test that the CollectionRulePipeline completes to due to action count limit.
        /// </summary>
        [Theory]
        [MemberData(nameof(GetTfmsSupportingPortListener))]
        public Task CollectionRulePipeline_ActionCountLimitUnlimitedDurationTest(TargetFrameworkMoniker appTfm)
        {
            const int ExpectedActionExecutionCount = 3;
            TimeSpan ClockIncrementDuration = TimeSpan.FromMilliseconds(10);

            MockTimeProvider timeProvider = new();
            ManualTriggerService triggerService = new();
            CallbackActionService callbackService = new(_outputHelper, timeProvider);

            return CollectionRulePipelineTestsHelper.ExecuteScenario(
                appTfm,
                TestAppScenarios.AsyncWait.Name,
                TestRuleName,
                options =>
                {
                    options.CreateCollectionRule(TestRuleName)
                        .SetManualTrigger()
                        .AddAction(CallbackAction.ActionName)
                        .SetActionLimits(count: ExpectedActionExecutionCount);
                },
                async (runner, pipeline, callbacks) =>
                {
                    using CancellationTokenSource cancellationSource = new(DefaultPipelineTimeout);

                    Task startedTask = callbacks.StartWaitForPipelineStarted();

                    Task runTask = pipeline.RunAsync(cancellationSource.Token);

                    await startedTask.WithCancellation(cancellationSource.Token);

                    await CollectionRulePipelineTestsHelper.ManualTriggerAsync(
                        triggerService,
                        callbackService,
                        callbacks,
                        ExpectedActionExecutionCount,
                        ExpectedActionExecutionCount,
                        timeProvider,
                        ClockIncrementDuration,
                        completesOnLastExpectedIteration: true,
                        cancellationSource.Token);

                    // Pipeline should run to completion due to action count limit without sliding window.
                    await runTask.WithCancellation(cancellationSource.Token);

                    await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);

                    // Action list should have been executed the expected number of times
                    VerifyExecutionCount(callbackService, ExpectedActionExecutionCount);
                },
                _outputHelper,
                services =>
                {
                    services.AddSingleton<TimeProvider>(timeProvider);
                    services.RegisterManualTrigger(triggerService);
                    services.RegisterTestAction(callbackService);
                });
        }

        /// <summary>
        /// Test that the CollectionRulePipeline throttles actions when action count limit is reached within window.
        /// </summary>
        [Theory]
        [MemberData(nameof(GetTfmsSupportingPortListener))]
        public Task CollectionRulePipeline_ActionCountLimitSlidingDurationTest(TargetFrameworkMoniker appTfm)
        {
            const int IterationCount = 5;
            const int ExpectedActionExecutionCount = 3;
            TimeSpan SlidingWindowDuration = TimeSpan.FromMilliseconds(100);
            TimeSpan ClockIncrementDuration = TimeSpan.FromMilliseconds(10);

            MockTimeProvider timeProvider = new();
            ManualTriggerService triggerService = new();
            CallbackActionService callbackService = new(_outputHelper, timeProvider);

            return CollectionRulePipelineTestsHelper.ExecuteScenario(
                appTfm,
                TestAppScenarios.AsyncWait.Name,
                TestRuleName,
                options =>
                {
                    options.CreateCollectionRule(TestRuleName)
                        .SetManualTrigger()
                        .AddAction(CallbackAction.ActionName)
                        .SetActionLimits(
                            count: ExpectedActionExecutionCount,
                            slidingWindowDuration: SlidingWindowDuration);
                },
                async (runner, pipeline, callbacks) =>
                {
                    using CancellationTokenSource cancellationSource = new(DefaultPipelineTimeout);

                    Task startedTask = callbacks.StartWaitForPipelineStarted();

                    Task runTask = pipeline.RunAsync(cancellationSource.Token);

                    await startedTask.WithCancellation(cancellationSource.Token);

                    await CollectionRulePipelineTestsHelper.ManualTriggerAsync(
                        triggerService,
                        callbackService,
                        callbacks,
                        IterationCount,
                        ExpectedActionExecutionCount,
                        timeProvider,
                        ClockIncrementDuration,
                        completesOnLastExpectedIteration: false,
                        cancellationSource.Token);

                    // Action list should have been executed the expected number of times
                    VerifyExecutionCount(callbackService, ExpectedActionExecutionCount);

                    timeProvider.Increment(2 * SlidingWindowDuration);

                    await CollectionRulePipelineTestsHelper.ManualTriggerAsync(
                        triggerService,
                        callbackService,
                        callbacks,
                        IterationCount,
                        ExpectedActionExecutionCount,
                        timeProvider,
                        ClockIncrementDuration,
                        completesOnLastExpectedIteration: false,
                        cancellationSource.Token);

                    // Expect total action invocation count to be twice the limit
                    VerifyExecutionCount(callbackService, 2 * ExpectedActionExecutionCount);

                    // Pipeline should not run to completion due to sliding window existence.
                    Assert.False(runTask.IsCompleted);

                    await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);

                    await pipeline.StopAsync(cancellationSource.Token);
                },
                _outputHelper,
                services =>
                {
                    services.AddSingleton<TimeProvider>(timeProvider);
                    services.RegisterManualTrigger(triggerService);
                    services.RegisterTestAction(callbackService);
                });
        }

        /// <summary>
        /// Writes the list of action execution timestamps to the output log.
        /// </summary>
        private void VerifyExecutionCount(CallbackActionService service, int expectedCount)
        {
            _outputHelper.WriteLine("Action execution times:");
            foreach (DateTime timestamp in service.ExecutionTimestamps)
            {
                _outputHelper.WriteLine("- {0}", timestamp.TimeOfDay);
            }

            Assert.Equal(expectedCount, service.ExecutionTimestamps.Count);
        }

        private static async Task ManualTriggerBurstAsync(ManualTriggerService service, int count = 10)
        {
            for (int i = 0; i < count; i++)
            {
                service.NotifyTriggerSubscribers();
                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }
        }

        public static IEnumerable<object[]> GetTfmsSupportingPortListener()
        {
            yield return new object[] { TargetFrameworkMoniker.Net60 };
            yield return new object[] { TargetFrameworkMoniker.Net70 };
            yield return new object[] { TargetFrameworkMoniker.Net80 };
        }

        public static IEnumerable<object[]> GetCurrentTfm()
        {
            yield return new object[] { TargetFrameworkMoniker.Net80 };
        }
    }
}
