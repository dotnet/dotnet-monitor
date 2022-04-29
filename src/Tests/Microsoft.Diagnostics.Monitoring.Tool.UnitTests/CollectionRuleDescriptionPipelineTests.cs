// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.Tool.UnitTests.CollectionRules.Actions;
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
    // Test Coverage:
    // Running
    // Running -> Throttled -> Running
    // Running -> Action Executing -> Running
    // Finished (Startup)
    // Finished (Rule Duration)
    // Finished (Action Count)

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

            return CollectionRulePipelineTests.ExecuteScenario(
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

                    // Since the action list was completed before the pipeline finished starting,
                    // the action should have invoked its callback.
                    await actionStarted1Task.WithCancellation(cancellationSource.Token);

                    // Pipeline should have completed shortly after finished starting. This should only
                    // wait for a very short time, if at all.
                    await runTask.WithCancellation(cancellationSource.Token);

                    CollectionRuleDescription actualDescription = CollectionRuleService.GetCollectionRuleDescription(pipeline);

                    CollectionRuleDescription expectedDescription = new()
                    {
                        ActionCountLimit = 5,
                        LifetimeOccurrences = 1,
                        SlidingWindowOccurrences = 1,
                        State = CollectionRulesState.Finished,
                        StateReason = CollectionRulesStateReasons.Finished_Startup
                    };

                    Assert.Equal(actualDescription, expectedDescription);

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
        [Theory]
        [MemberData(nameof(CollectionRulePipelineTests.GetTfmsSupportingPortListener), MemberType = typeof(CollectionRulePipelineTests))]
        public Task CollectionRuleDescriptionPipeline_ExecutingAction(TargetFrameworkMoniker appTfm)
        {
            TimeSpan ClockIncrementDuration = TimeSpan.FromMilliseconds(10);

            MockSystemClock clock = new();
            ManualTriggerService triggerService = new();
            CallbackActionService callbackService = new(_outputHelper, clock);

            using TemporaryDirectory tempDirectory = new(_outputHelper);

            return CollectionRulePipelineTests.ExecuteScenario(
                appTfm,
                TestAppScenarios.AsyncWait.Name,
                TestRuleName,
                options =>
                {
                    options.CreateCollectionRule(TestRuleName)
                        .SetManualTrigger()
                        .AddCollectDumpAction(ActionTestsConstants.ExpectedEgressProvider) // Having this seems to stabilize the test (presumably since it doesn't happen instantly)...don't trust the timing
                        .AddAction(CallbackAction.ActionName)
                        .SetActionLimits(count: ExpectedActionExecutionCount);

                    options.AddFileSystemEgress(ActionTestsConstants.ExpectedEgressProvider, tempDirectory.FullName);

                },
                async (runner, pipeline, callbacks) =>
                {
                    using CancellationTokenSource cancellationSource = new(DefaultPipelineTimeout);

                    Task startedTask = callbacks.StartWaitForPipelineStarted();

                    Task runTask = pipeline.RunAsync(cancellationSource.Token);

                    await startedTask.WithCancellation(cancellationSource.Token);

                    Task actionStartedTask;
                    Task actionsThrottledTask = callbacks.StartWaitForActionsThrottled();

                    TaskCompletionSource<object> startedSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
                    EventHandler startedHandler = (s, e) => startedSource.TrySetResult(null);
                    using var _ = cancellationSource.Token.Register(() => startedSource.TrySetCanceled(cancellationSource.Token));

                    actionStartedTask = await callbackService.StartWaitForCallbackAsync(cancellationSource.Token);

                    triggerService.NotifyStarted += startedHandler;

                    // Manually invoke the trigger.
                    triggerService.NotifyTriggerSubscribers();

                    // Wait until action has started.
                    await actionStartedTask.WithCancellation(cancellationSource.Token);

                    CollectionRuleDescription actualDescription = CollectionRuleService.GetCollectionRuleDescription(pipeline);

                    CollectionRuleDescription expectedDescription = new()
                    {
                        ActionCountLimit = ExpectedActionExecutionCount,
                        LifetimeOccurrences = 1,
                        SlidingWindowOccurrences = 1,
                        State = CollectionRulesState.ActionExecuting,
                        StateReason = CollectionRulesStateReasons.ExecutingActions
                    };

                    Assert.Equal(actualDescription, expectedDescription);

                    await startedSource.WithCancellation(cancellationSource.Token);

                    CollectionRuleDescription actualDescription2 = CollectionRuleService.GetCollectionRuleDescription(pipeline);

                    CollectionRuleDescription expectedDescription2 = new()
                    {
                        ActionCountLimit = ExpectedActionExecutionCount,
                        LifetimeOccurrences = 1,
                        SlidingWindowOccurrences = 1,
                        State = CollectionRulesState.Running,
                        StateReason = CollectionRulesStateReasons.Running
                    };

                    Assert.Equal(actualDescription2, expectedDescription2);

                    triggerService.NotifyStarted -= startedHandler;

                    /////////////////////////////////

                    await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);

                    await pipeline.StopAsync(cancellationSource.Token);
                },
                _outputHelper,
                services =>
                {
                    services.AddSingleton<ISystemClock>(clock);
                    services.RegisterManualTrigger(triggerService);
                    services.RegisterTestAction(callbackService);
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
            TimeSpan SlidingWindowDuration = TimeSpan.FromMilliseconds(2000); // NOTE: A value greater than 1 second is necessary since the Countdown trims precision to the nearest second (for user-readability)
            TimeSpan ClockIncrementDuration = TimeSpan.FromMilliseconds(10);

            MockSystemClock clock = new();
            ManualTriggerService triggerService = new();
            CallbackActionService callbackService = new(_outputHelper, clock);

            return CollectionRulePipelineTests.ExecuteScenario(
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

                    await CollectionRulePipelineTests.ManualTriggerAsync(
                        triggerService,
                        callbackService,
                        callbacks,
                        IterationCount,
                        ExpectedActionExecutionCount,
                        clock,
                        ClockIncrementDuration,
                        completesOnLastExpectedIteration: false,
                        cancellationSource.Token);

                    CollectionRuleDescription actualDescription1 = CollectionRuleService.GetCollectionRuleDescription(pipeline);

                    CollectionRuleDescription expectedDescription1 = new()
                    {
                        ActionCountLimit = ExpectedActionExecutionCount,
                        ActionCountSlidingWindowDurationLimit = SlidingWindowDuration,
                        LifetimeOccurrences = ExpectedActionExecutionCount,
                        SlidingWindowOccurrences = ExpectedActionExecutionCount,
                        State = CollectionRulesState.Throttled,
                        StateReason = CollectionRulesStateReasons.Throttled,
                        SlidingWindowDurationCountdown = TimeSpan.Parse("00:00:01") // Rounding due to (intentional) lost precision
                    };

                    Assert.Equal(actualDescription1, expectedDescription1);

                    clock.Increment(2 * SlidingWindowDuration);

                    CollectionRuleDescription actualDescription2 = CollectionRuleService.GetCollectionRuleDescription(pipeline);

                    CollectionRuleDescription expectedDescription2 = new()
                    {
                        ActionCountLimit = ExpectedActionExecutionCount,
                        ActionCountSlidingWindowDurationLimit = SlidingWindowDuration,
                        LifetimeOccurrences = ExpectedActionExecutionCount,
                        SlidingWindowOccurrences = 0,
                        State = CollectionRulesState.Running,
                        StateReason = CollectionRulesStateReasons.Running,
                    };

                    Assert.Equal(actualDescription2, expectedDescription2);

                    await CollectionRulePipelineTests.ManualTriggerAsync(
                        triggerService,
                        callbackService,
                        callbacks,
                        IterationCount,
                        ExpectedActionExecutionCount,
                        clock,
                        ClockIncrementDuration,
                        completesOnLastExpectedIteration: false,
                        cancellationSource.Token);

                    // Pipeline should not run to completion due to sliding window existance.
                    Assert.False(runTask.IsCompleted);

                    CollectionRuleDescription actualDescription3 = CollectionRuleService.GetCollectionRuleDescription(pipeline);

                    CollectionRuleDescription expectedDescription3 = new()
                    {
                        ActionCountLimit = ExpectedActionExecutionCount,
                        ActionCountSlidingWindowDurationLimit = SlidingWindowDuration,
                        LifetimeOccurrences = 2 * ExpectedActionExecutionCount,
                        SlidingWindowOccurrences = ExpectedActionExecutionCount,
                        State = CollectionRulesState.Throttled,
                        StateReason = CollectionRulesStateReasons.Throttled,
                        SlidingWindowDurationCountdown = TimeSpan.Parse("00:00:01") // Rounding due to (intentional) lost precision
                    };

                    Assert.Equal(actualDescription3, expectedDescription3);

                    await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);

                    await pipeline.StopAsync(cancellationSource.Token);
                },
                _outputHelper,
                services =>
                {
                    services.AddSingleton<ISystemClock>(clock);
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

            return CollectionRulePipelineTests.ExecuteScenario(
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

                    CollectionRuleDescription actualDescription = CollectionRuleService.GetCollectionRuleDescription(pipeline);

                    CollectionRuleDescription expectedDescription = new()
                    {
                        ActionCountLimit = CollectionRuleLimitsOptionsDefaults.ActionCount,
                        LifetimeOccurrences = 0,
                        SlidingWindowOccurrences = 0,
                        State = CollectionRulesState.Finished,
                        StateReason = CollectionRulesStateReasons.Finished_RuleDuration,
                    };

                    Assert.Equal(actualDescription, expectedDescription);
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

            MockSystemClock clock = new();
            ManualTriggerService triggerService = new();
            CallbackActionService callbackService = new(_outputHelper, clock);

            return CollectionRulePipelineTests.ExecuteScenario(
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

                    await CollectionRulePipelineTests.ManualTriggerAsync(
                        triggerService,
                        callbackService,
                        callbacks,
                        ExpectedActionExecutionCount,
                        ExpectedActionExecutionCount,
                        clock,
                        ClockIncrementDuration,
                        completesOnLastExpectedIteration: true,
                        cancellationSource.Token);

                    // Pipeline should run to completion due to action count limit without sliding window.
                    await runTask.WithCancellation(cancellationSource.Token);

                    await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);

                    CollectionRuleDescription actualDescription = CollectionRuleService.GetCollectionRuleDescription(pipeline);

                    CollectionRuleDescription expectedDescription = new()
                    {
                        ActionCountLimit = ExpectedActionExecutionCount,
                        LifetimeOccurrences = ExpectedActionExecutionCount,
                        SlidingWindowOccurrences = ExpectedActionExecutionCount,
                        State = CollectionRulesState.Finished,
                        StateReason = CollectionRulesStateReasons.Finished_ActionCount,
                    };

                    Assert.Equal(actualDescription, expectedDescription);
                },
                _outputHelper,
                services =>
                {
                    services.AddSingleton<ISystemClock>(clock);
                    services.RegisterManualTrigger(triggerService);
                    services.RegisterTestAction(callbackService);
                });
        }
    }
}
