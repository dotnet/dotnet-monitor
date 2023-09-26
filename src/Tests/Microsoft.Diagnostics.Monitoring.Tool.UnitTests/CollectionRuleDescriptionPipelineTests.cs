// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.Tool.UnitTests.CollectionRules.Triggers;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public class CollectionRuleDescriptionPipelineTests
    {
        private readonly TimeSpan DefaultPipelineTimeout = TimeSpan.FromSeconds(30);
        private const string TestRuleName = "TestPipelineRule";
        const int ExpectedActionExecutionCount = 3;

        private readonly ITestOutputHelper _outputHelper;

        public CollectionRuleDescriptionPipelineTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        /// <summary>
        /// Test for the Finished (Startup) state.
        /// </summary>
        [Theory]
        [MemberData(nameof(CollectionRulePipelineTests.GetTfmsSupportingPortListener), MemberType = typeof(CollectionRulePipelineTests))]
        public Task CollectionRuleDescriptionPipeline_StartupTriggerTest(TargetFrameworkMoniker appTfm)
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
                    Task actionStartedTask = await callbackService.StartWaitForCallbackAsync(cancellationSource.Token);

                    // Startup trigger will cause the the pipeline to complete the start phase
                    // after the action list has been completed.
                    Task runTask = pipeline.RunAsync(cancellationSource.Token);

                    await startedTask.WithCancellation(cancellationSource.Token);

                    // Since the action list was completed before the pipeline finished starting,
                    // the action should have invoked its callback.
                    await actionStartedTask.WithCancellation(cancellationSource.Token);

                    // Pipeline should have completed shortly after finished starting. This should only
                    // wait for a very short time, if at all.
                    await runTask.WithCancellation(cancellationSource.Token);

                    CompareCollectionRuleDetailedDescriptions(pipeline, new()
                    {
                        ActionCountLimit = CollectionRuleLimitsOptionsDefaults.ActionCount,
                        LifetimeOccurrences = 1,
                        SlidingWindowOccurrences = 1,
                        State = CollectionRuleState.Finished,
                        StateReason = Strings.Message_CollectionRuleStateReason_Finished_Startup
                    }, TestRuleName);

                    await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                },
                _outputHelper,
                services =>
                {
                    services.RegisterTestAction(callbackService);
                });
        }

        /// <summary>
        /// Test for Executing Action state.
        /// </summary>
        [Theory(Skip = "https://github.com/dotnet/dotnet-monitor/issues/2241")]
        [MemberData(nameof(CollectionRulePipelineTests.GetTfmsSupportingPortListener), MemberType = typeof(CollectionRulePipelineTests))]
        public Task CollectionRuleDescriptionPipeline_ExecutingAction(TargetFrameworkMoniker appTfm)
        {
            TimeSpan ClockIncrementDuration = TimeSpan.FromMilliseconds(10);

            MockTimeProvider timeProvider = new();
            ManualTriggerService triggerService = new();
            CallbackActionService callbackService = new(_outputHelper, timeProvider);

            using TemporaryDirectory tempDirectory = new(_outputHelper);

            return CollectionRulePipelineTestsHelper.ExecuteScenario(
                appTfm,
                TestAppScenarios.AsyncWait.Name,
                TestRuleName,
                options =>
                {
                    options.CreateCollectionRule(TestRuleName)
                        .SetManualTrigger()
                        .AddAction(DelayedCallbackAction.ActionName)
                        .SetActionLimits(count: ExpectedActionExecutionCount);

                    options.AddFileSystemEgress(ActionTestsConstants.ExpectedEgressProvider, tempDirectory.FullName);
                },
                async (runner, pipeline, callbacks) =>
                {
                    using CancellationTokenSource cancellationSource = new(DefaultPipelineTimeout);

                    Task startedTask = callbacks.StartWaitForPipelineStarted();

                    Task runTask = pipeline.RunAsync(cancellationSource.Token);

                    await startedTask.WithCancellation(cancellationSource.Token);

                    Task actionsThrottledTask = callbacks.StartWaitForActionsThrottled();

                    // Borrowed portions of this from ManualTriggerAsync implementation
                    TaskCompletionSource<object> startedSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
                    EventHandler startedHandler = (s, e) => startedSource.TrySetResult(null);
                    using var _ = cancellationSource.Token.Register(() => startedSource.TrySetCanceled(cancellationSource.Token));

                    Task actionStartedTask = await callbackService.StartWaitForCallbackAsync(cancellationSource.Token);

                    triggerService.NotifyStarted += startedHandler;

                    // Manually invoke the trigger.
                    triggerService.NotifyTriggerSubscribers();

                    // Wait until action has started.
                    await actionStartedTask.WithCancellation(cancellationSource.Token);

                    CompareCollectionRuleDetailedDescriptions(pipeline, new()
                    {
                        ActionCountLimit = ExpectedActionExecutionCount,
                        LifetimeOccurrences = 1,
                        SlidingWindowOccurrences = 1,
                        State = CollectionRuleState.ActionExecuting,
                        StateReason = Strings.Message_CollectionRuleStateReason_ExecutingActions
                    }, TestRuleName);

                    timeProvider.Increment(ClockIncrementDuration);

                    await startedSource.WithCancellation(cancellationSource.Token);

                    CompareCollectionRuleDetailedDescriptions(pipeline, new()
                    {
                        ActionCountLimit = ExpectedActionExecutionCount,
                        LifetimeOccurrences = 1,
                        SlidingWindowOccurrences = 1,
                        State = CollectionRuleState.Running,
                        StateReason = Strings.Message_CollectionRuleStateReason_Running
                    }, TestRuleName);

                    triggerService.NotifyStarted -= startedHandler;

                    await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);

                    await pipeline.StopAsync(cancellationSource.Token);
                },
                _outputHelper,
                services =>
                {
                    services.AddSingleton<TimeProvider>(timeProvider);
                    services.RegisterManualTrigger(triggerService);
                    services.RegisterDelayedTestAction(callbackService);
                });
        }

        /// <summary>
        /// Test for Throttled -> Running -> Throttled.
        /// </summary>
        [Theory]
        [MemberData(nameof(CollectionRulePipelineTests.GetTfmsSupportingPortListener), MemberType = typeof(CollectionRulePipelineTests))]
        public Task CollectionRuleDescriptionPipeline_Throttled(TargetFrameworkMoniker appTfm)
        {
            const int IterationCount = 5;
            TimeSpan SlidingWindowDuration = TimeSpan.FromSeconds(2); // NOTE: A value greater than 1 second is necessary since the countdown trims precision to the nearest second (for user-readability)
            TimeSpan ExpectedSlidingWindowDurationCountdown = TimeSpan.FromSeconds(1);
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

                    CompareCollectionRuleDetailedDescriptions(pipeline, new()
                    {
                        ActionCountLimit = ExpectedActionExecutionCount,
                        ActionCountSlidingWindowDurationLimit = SlidingWindowDuration,
                        LifetimeOccurrences = ExpectedActionExecutionCount,
                        SlidingWindowOccurrences = ExpectedActionExecutionCount,
                        State = CollectionRuleState.Throttled,
                        StateReason = Strings.Message_CollectionRuleStateReason_Throttled,
                        SlidingWindowDurationCountdown = ExpectedSlidingWindowDurationCountdown
                    }, TestRuleName);

                    timeProvider.Increment(2 * SlidingWindowDuration);

                    CompareCollectionRuleDetailedDescriptions(pipeline, new()
                    {
                        ActionCountLimit = ExpectedActionExecutionCount,
                        ActionCountSlidingWindowDurationLimit = SlidingWindowDuration,
                        LifetimeOccurrences = ExpectedActionExecutionCount,
                        SlidingWindowOccurrences = 0,
                        State = CollectionRuleState.Running,
                        StateReason = Strings.Message_CollectionRuleStateReason_Running
                    }, TestRuleName);

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

                    CompareCollectionRuleDetailedDescriptions(pipeline, new()
                    {
                        ActionCountLimit = ExpectedActionExecutionCount,
                        ActionCountSlidingWindowDurationLimit = SlidingWindowDuration,
                        LifetimeOccurrences = 2 * ExpectedActionExecutionCount,
                        SlidingWindowOccurrences = ExpectedActionExecutionCount,
                        State = CollectionRuleState.Throttled,
                        StateReason = Strings.Message_CollectionRuleStateReason_Throttled,
                        SlidingWindowDurationCountdown = ExpectedSlidingWindowDurationCountdown
                    }, TestRuleName);

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
        /// Test for the Finished (Rule Duration) state.
        /// </summary>
        [Theory]
        [MemberData(nameof(CollectionRulePipelineTests.GetTfmsSupportingPortListener), MemberType = typeof(CollectionRulePipelineTests))]
        public Task CollectionRuleDescriptionPipeline_ReachedRuleDuration(TargetFrameworkMoniker appTfm)
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

                    CompareCollectionRuleDetailedDescriptions(pipeline, new()
                    {
                        ActionCountLimit = CollectionRuleLimitsOptionsDefaults.ActionCount,
                        LifetimeOccurrences = 0,
                        SlidingWindowOccurrences = 0,
                        State = CollectionRuleState.Finished,
                        StateReason = Strings.Message_CollectionRuleStateReason_Finished_RuleDuration
                    }, TestRuleName);
                },
                _outputHelper,
                services =>
                {
                    services.RegisterManualTrigger(triggerService);
                    services.RegisterTestAction(callbackService);
                });
        }

        /// <summary>
        /// Test for the Finished (Action Count Limit) state.
        /// </summary>
        [Theory]
        [MemberData(nameof(CollectionRulePipelineTests.GetTfmsSupportingPortListener), MemberType = typeof(CollectionRulePipelineTests))]
        public Task CollectionRuleDescriptionPipeline_ActionCountLimitUnlimitedDurationTest(TargetFrameworkMoniker appTfm)
        {
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

                    CompareCollectionRuleDetailedDescriptions(pipeline, new()
                    {
                        ActionCountLimit = ExpectedActionExecutionCount,
                        LifetimeOccurrences = ExpectedActionExecutionCount,
                        SlidingWindowOccurrences = ExpectedActionExecutionCount,
                        State = CollectionRuleState.Finished,
                        StateReason = Strings.Message_CollectionRuleStateReason_Finished_ActionCount
                    }, TestRuleName);
                },
                _outputHelper,
                services =>
                {
                    services.AddSingleton<TimeProvider>(timeProvider);
                    services.RegisterManualTrigger(triggerService);
                    services.RegisterTestAction(callbackService);
                });
        }

        private static void CompareCollectionRuleDescriptions(CollectionRulePipeline pipeline, CollectionRuleDescription expectedDescription)
        {
            CollectionRuleDescription actualDescription = CollectionRuleService.GetCollectionRuleDescription(pipeline);

            Assert.Equal(actualDescription, expectedDescription);
        }

        private static void CompareCollectionRuleDescriptions(CollectionRulePipeline pipeline, CollectionRuleDetailedDescription expectedDetailedDescription)
        {
            CompareCollectionRuleDescriptions(pipeline, new CollectionRuleDescription()
            {
                State = expectedDetailedDescription.State,
                StateReason = expectedDetailedDescription.StateReason
            });
        }


        private static void CompareCollectionRuleDetailedDescriptions(CollectionRulePipeline pipeline, CollectionRuleDetailedDescription expectedDetailedDescription, string collectionRuleName)
        {
            CompareCollectionRuleDescriptions(pipeline, expectedDetailedDescription);

            CollectionRuleDetailedDescription actualDetailedDescription = CollectionRuleService.GetCollectionRuleDetailedDescription(pipeline);

            Assert.Equal(expectedDetailedDescription, actualDetailedDescription);
        }
    }
}
