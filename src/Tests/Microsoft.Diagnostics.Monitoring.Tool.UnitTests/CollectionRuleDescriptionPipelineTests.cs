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
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Triggers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Reflection;
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
    // 

    public class CollectionRuleDescriptionPipelineTests
    {
        private readonly TimeSpan DefaultPipelineTimeout = TimeSpan.FromSeconds(30);
        private const string TestRuleName = "TestPipelineRule";

        private readonly ITestOutputHelper _outputHelper;

        public CollectionRuleDescriptionPipelineTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        /// <summary>
        /// Test for the Finished (Startup) state.
        /// </summary>
        [Theory]
        [MemberData(nameof(GetTfmsSupportingPortListener))]
        public Task CollectionRulePipeline_StartupTriggerTest(TargetFrameworkMoniker appTfm)
        {
            CallbackActionService callbackService = new(_outputHelper);

            return ExecuteScenario(
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
                    // the action should have invoked it's callback.
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

                    ValidateCollectionRuleDescriptions(actualDescription, expectedDescription);

                    await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                },
                services =>
                {
                    services.RegisterTestAction(callbackService);
                });
        }

        /// <summary>
        /// Test for Executing Action state.
        /// </summary>
        [Theory]
        [MemberData(nameof(GetTfmsSupportingPortListener))]
        public Task CollectionRuleDescriptionPipeline_ExecutingAction(TargetFrameworkMoniker appTfm)
        {
            const int ExpectedActionExecutionCount = 3;
            TimeSpan ClockIncrementDuration = TimeSpan.FromMilliseconds(10);

            MockSystemClock clock = new();
            ManualTriggerService triggerService = new();
            CallbackActionService callbackService = new(_outputHelper, clock);

            return ExecuteScenario(
                appTfm,
                TestAppScenarios.AsyncWait.Name,
                TestRuleName,
                options =>
                {
                    options.CreateCollectionRule(TestRuleName)
                        .SetManualTrigger()
                        .AddAction(CallbackAction.ActionName)
                        .SetActionLimits(
                            count: ExpectedActionExecutionCount
                            );
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

                    ValidateCollectionRuleDescriptions(actualDescription, expectedDescription);

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

                    ValidateCollectionRuleDescriptions(actualDescription2, expectedDescription2);

                    triggerService.NotifyStarted -= startedHandler;

                    // Advance the clock source.
                    clock.Increment(ClockIncrementDuration);

                    // Check that actions were not throttled.
                    Assert.False(actionsThrottledTask.IsCompleted);

                    actionStartedTask = await callbackService.StartWaitForCallbackAsync(cancellationSource.Token);

                    // Check that no actions have been executed.
                    Assert.False(actionStartedTask.IsCompleted);

                    /////////////////////////////////

                    VerifyExecutionCount(callbackService, 1);

                    // Pipeline should not run to completion due to sliding window existance.
                    Assert.False(runTask.IsCompleted);

                    await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);

                    await pipeline.StopAsync(cancellationSource.Token);
                },
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
        [MemberData(nameof(GetTfmsSupportingPortListener))]
        public Task CollectionRuleDescriptionPipeline_Throttled(TargetFrameworkMoniker appTfm)
        {
            const int IterationCount = 5;
            const int ExpectedActionExecutionCount = 3;
            TimeSpan SlidingWindowDuration = TimeSpan.FromMilliseconds(2000); // NOTE: A value greater than 1 second is necessary since the Countdown trims precision to the nearest second (for user-readability)
            TimeSpan ClockIncrementDuration = TimeSpan.FromMilliseconds(10);

            MockSystemClock clock = new();
            ManualTriggerService triggerService = new();
            CallbackActionService callbackService = new(_outputHelper, clock);

            return ExecuteScenario(
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

                    await ManualTriggerAsync(
                        triggerService,
                        callbackService,
                        callbacks,
                        IterationCount,
                        ExpectedActionExecutionCount,
                        clock,
                        ClockIncrementDuration,
                        completesOnLastExpectedIteration: false,
                        cancellationSource.Token);

                    // Action list should have been executed the expected number of times
                    VerifyExecutionCount(callbackService, ExpectedActionExecutionCount);

                    CollectionRuleDescription actualDescription1 = CollectionRuleService.GetCollectionRuleDescription(pipeline);

                    CollectionRuleDescription expectedDescription1 = new()
                    {
                        ActionCountLimit = ExpectedActionExecutionCount,
                        ActionCountSlidingWindowDurationLimit = SlidingWindowDuration,
                        LifetimeOccurrences = ExpectedActionExecutionCount,
                        SlidingWindowOccurrences = ExpectedActionExecutionCount,
                        State = CollectionRulesState.Throttled,
                        StateReason = CollectionRulesStateReasons.Throttled,
                        SlidingWindowDurationCountdown = TimeSpan.Parse("00:00:01")
                    };

                    ValidateCollectionRuleDescriptions(actualDescription1, expectedDescription1);

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

                    ValidateCollectionRuleDescriptions(actualDescription2, expectedDescription2);

                    await ManualTriggerAsync(
                        triggerService,
                        callbackService,
                        callbacks,
                        IterationCount,
                        ExpectedActionExecutionCount,
                        clock,
                        ClockIncrementDuration,
                        completesOnLastExpectedIteration: false,
                        cancellationSource.Token);

                    // Expect total action invocation count to be twice the limit
                    VerifyExecutionCount(callbackService, 2 * ExpectedActionExecutionCount);

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
                        SlidingWindowDurationCountdown = TimeSpan.Parse("00:00:01")
                    };

                    ValidateCollectionRuleDescriptions(actualDescription3, expectedDescription3);

                    await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);

                    await pipeline.StopAsync(cancellationSource.Token);
                },
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
        [MemberData(nameof(GetTfmsSupportingPortListener))]
        public Task CollectionRuleDescriptionPipeline_ReachedRuleDuration(TargetFrameworkMoniker appTfm)
        {
            ManualTriggerService triggerService = new();
            CallbackActionService callbackService = new(_outputHelper);

            return ExecuteScenario(
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

                    CollectionRuleDescription actualDescription = CollectionRuleService.GetCollectionRuleDescription(pipeline);

                    CollectionRuleDescription expectedDescription = new()
                    {
                        ActionCountLimit = CollectionRuleLimitsOptionsDefaults.ActionCount,
                        LifetimeOccurrences = 0,
                        SlidingWindowOccurrences = 0,
                        State = CollectionRulesState.Finished,
                        StateReason = CollectionRulesStateReasons.Finished_RuleDuration,
                    };

                    ValidateCollectionRuleDescriptions(actualDescription, expectedDescription);
                },
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
        [MemberData(nameof(GetTfmsSupportingPortListener))]
        public Task CollectionRulePipeline_ActionCountLimitUnlimitedDurationTest(TargetFrameworkMoniker appTfm)
        {
            const int ExpectedActionExecutionCount = 3;
            TimeSpan ClockIncrementDuration = TimeSpan.FromMilliseconds(10);

            MockSystemClock clock = new();
            ManualTriggerService triggerService = new();
            CallbackActionService callbackService = new(_outputHelper, clock);

            return ExecuteScenario(
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

                    await ManualTriggerAsync(
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

                    // Action list should have been executed the expected number of times
                    VerifyExecutionCount(callbackService, ExpectedActionExecutionCount);

                    CollectionRuleDescription actualDescription = CollectionRuleService.GetCollectionRuleDescription(pipeline);

                    CollectionRuleDescription expectedDescription = new()
                    {
                        ActionCountLimit = ExpectedActionExecutionCount,
                        LifetimeOccurrences = ExpectedActionExecutionCount,
                        SlidingWindowOccurrences = ExpectedActionExecutionCount,
                        State = CollectionRulesState.Finished,
                        StateReason = CollectionRulesStateReasons.Finished_ActionCount,
                    };

                    ValidateCollectionRuleDescriptions(actualDescription, expectedDescription);
                },
                services =>
                {
                    services.AddSingleton<ISystemClock>(clock);
                    services.RegisterManualTrigger(triggerService);
                    services.RegisterTestAction(callbackService);
                });
        }




        private void ValidateCollectionRuleDescriptions(CollectionRuleDescription actualDescription, CollectionRuleDescription expectedDescription)
        {
            Assert.Equal(expectedDescription.ActionCountLimit, actualDescription.ActionCountLimit);
            Assert.Equal(expectedDescription.ActionCountSlidingWindowDurationLimit, actualDescription.ActionCountSlidingWindowDurationLimit);
            Assert.Equal(expectedDescription.LifetimeOccurrences, actualDescription.LifetimeOccurrences);
            Assert.Equal(expectedDescription.RuleFinishedCountdown, actualDescription.RuleFinishedCountdown);
            Assert.Equal(expectedDescription.SlidingWindowDurationCountdown, actualDescription.SlidingWindowDurationCountdown);
            Assert.Equal(expectedDescription.SlidingWindowOccurrences, actualDescription.SlidingWindowOccurrences);
            Assert.Equal(expectedDescription.State, actualDescription.State);
            Assert.Equal(expectedDescription.StateReason, actualDescription.StateReason);
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

        /// <summary>
        /// Manually trigger for a number of iterations (<paramref name="iterationCount"/>) and test
        /// that the actions are invoked for the number of expected iterations (<paramref name="expectedCount"/>) and
        /// are throttled for the remaining number of iterations.
        /// </summary>
        private async Task ManualTriggerAsync(
            ManualTriggerService triggerService,
            CallbackActionService callbackService,
            PipelineCallbacks callbacks,
            int iterationCount,
            int expectedCount,
            MockSystemClock clock,
            TimeSpan clockIncrementDuration,
            bool completesOnLastExpectedIteration,
            CancellationToken token)
        {
            if (iterationCount < expectedCount)
            {
                throw new InvalidOperationException("Number of iterations must be greater than or equal to number of expected iterations.");
            }

            int iteration = 0;
            Task actionStartedTask;
            Task actionsThrottledTask = callbacks.StartWaitForActionsThrottled();

            // Test that the actions are run for each iteration where the actions are expected to run.
            while (iteration < expectedCount)
            {
                iteration++;

                TaskCompletionSource<object> startedSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
                EventHandler startedHandler = (s, e) => startedSource.TrySetResult(null);
                using var _ = token.Register(() => startedSource.TrySetCanceled(token));

                actionStartedTask = await callbackService.StartWaitForCallbackAsync(token);

                triggerService.NotifyStarted += startedHandler;

                // Manually invoke the trigger.
                triggerService.NotifyTriggerSubscribers();

                // Wait until action has started.
                await actionStartedTask.WithCancellation(token);

                // If the pipeline completes on the last expected iteration, the trigger will not be started again.
                // Skip this check for the last expected iteration if the pipeline is expected to complete.
                if (!completesOnLastExpectedIteration || iteration != expectedCount)
                {
                    await startedSource.WithCancellation(token);
                }

                triggerService.NotifyStarted -= startedHandler;

                // Advance the clock source.
                clock.Increment(clockIncrementDuration);
            }

            // Check that actions were not throttled.
            Assert.False(actionsThrottledTask.IsCompleted);

            actionStartedTask = await callbackService.StartWaitForCallbackAsync(token);

            // Test that actions are throttled for remaining iterations.
            while (iteration < iterationCount)
            {
                iteration++;

                TaskCompletionSource<object> startedSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
                EventHandler startedHandler = (s, e) => startedSource.TrySetResult(null);
                using var _ = token.Register(() => startedSource.TrySetCanceled(token));

                actionsThrottledTask = callbacks.StartWaitForActionsThrottled();

                triggerService.NotifyStarted += startedHandler;

                // Manually invoke the trigger.
                triggerService.NotifyTriggerSubscribers();

                // Check throttling has occurred.
                await actionsThrottledTask.WithCancellation(token);

                await startedSource.WithCancellation(token);

                triggerService.NotifyStarted -= startedHandler;

                // Advance the clock source.
                clock.Increment(clockIncrementDuration);
            }

            // Check that no actions have been executed.
            Assert.False(actionStartedTask.IsCompleted);
        }

        private async Task ManualTriggerBurstAsync(ManualTriggerService service, int count = 10)
        {
            for (int i = 0; i < count; i++)
            {
                service.NotifyTriggerSubscribers();
                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }
        }

        public static IEnumerable<object[]> GetTfmsSupportingPortListener()
        {
            yield return new object[] { TargetFrameworkMoniker.Net50 };
            yield return new object[] { TargetFrameworkMoniker.Net60 };
        }

        private async Task ExecuteScenario(
            TargetFrameworkMoniker tfm,
            string scenarioName,
            string collectionRuleName,
            Action<Tools.Monitor.RootOptions> setup,
            Func<AppRunner, CollectionRulePipeline, PipelineCallbacks, Task> pipelineCallback,
            Action<IServiceCollection> servicesCallback = null)
        {
            EndpointInfoSourceCallback endpointInfoCallback = new(_outputHelper);
            EndpointUtilities endpointUtilities = new(_outputHelper);
            await using ServerSourceHolder sourceHolder = await endpointUtilities.StartServerAsync(endpointInfoCallback);

            AppRunner runner = new(_outputHelper, Assembly.GetExecutingAssembly(), tfm: tfm);
            runner.ConnectionMode = DiagnosticPortConnectionMode.Connect;
            runner.DiagnosticPortPath = sourceHolder.TransportName;
            runner.ScenarioName = scenarioName;            

            Task<IEndpointInfo> endpointInfoTask = endpointInfoCallback.WaitAddedEndpointInfoAsync(runner, CommonTestTimeouts.StartProcess);

            await runner.ExecuteAsync(async () =>
            {
                IEndpointInfo endpointInfo = await endpointInfoTask;

                await TestHostHelper.CreateCollectionRulesHost(
                    _outputHelper,
                    setup,
                    async host =>
                    {
                        ActionListExecutor actionListExecutor =
                            host.Services.GetRequiredService<ActionListExecutor>();
                        ICollectionRuleTriggerOperations triggerOperations =
                            host.Services.GetRequiredService<ICollectionRuleTriggerOperations>();
                        IOptionsMonitor<CollectionRuleOptions> optionsMonitor =
                            host.Services.GetRequiredService<IOptionsMonitor<CollectionRuleOptions>>();
                        ILogger<CollectionRuleService> logger =
                            host.Services.GetRequiredService<ILogger<CollectionRuleService>>();
                        ISystemClock clock =
                            host.Services.GetRequiredService<ISystemClock>();

                        PipelineCallbacks callbacks = new();

                        CollectionRuleContext context = new(
                            collectionRuleName,
                            optionsMonitor.Get(collectionRuleName),
                            endpointInfo,
                            logger,
                            clock,
                            callbacks.NotifyActionsThrottled);

                        await using CollectionRulePipeline pipeline = new(
                            actionListExecutor,
                            triggerOperations,
                            context,
                            callbacks.NotifyPipelineStarted);

                        await pipelineCallback(runner, pipeline, callbacks);

                        Assert.Equal(1, callbacks.StartedCount);
                    },
                    servicesCallback);
            });
        }

        private class PipelineCallbacks
        {
            private readonly List<CompletionEntry> _entries = new();

            private int _startedCount;

            public Task StartWaitForPipelineStarted()
            {
                return RegisterCompletion(PipelineCallbackType.PipelineStarted);
            }

            public Task StartWaitForActionsThrottled()
            {
                return RegisterCompletion(PipelineCallbackType.ActionsThrottled);
            }

            public void NotifyActionsThrottled()
            {
                NotifyCompletions(PipelineCallbackType.ActionsThrottled);
            }

            public void NotifyPipelineStarted()
            {
                _startedCount++;
                NotifyCompletions(PipelineCallbackType.PipelineStarted);
            }

            private Task RegisterCompletion(PipelineCallbackType callbackType)
            {
                CompletionEntry entry = new(callbackType);
                lock (_entries)
                {
                    _entries.Add(entry);
                }
                return entry.CompletionTask;
            }

            private void NotifyCompletions(PipelineCallbackType callbackType)
            {
                List<CompletionEntry> matchingEntries;
                lock (_entries)
                {
                    matchingEntries = new(_entries.Count);
                    for (int i = 0; i < _entries.Count; i++)
                    {
                        CompletionEntry entry = _entries[i];
                        if (_entries[i].CallbackType == callbackType)
                        {
                            _entries.RemoveAt(i);
                            matchingEntries.Add(entry);
                            i--;
                        }
                    }
                }

                foreach (CompletionEntry entry in matchingEntries)
                {
                    entry.Complete();
                }
            }

            public int StartedCount => _startedCount;

            private class CompletionEntry
            {
                private readonly TaskCompletionSource<object> _source = new(TaskCreationOptions.RunContinuationsAsynchronously);

                public CompletionEntry(PipelineCallbackType callbackType)
                {
                    CallbackType = callbackType;
                }

                public void Complete()
                {
                    _source.TrySetResult(null);
                }

                public PipelineCallbackType CallbackType { get; }

                public Task CompletionTask => _source.Task;
            }

            private enum PipelineCallbackType
            {
                PipelineStarted,
                ActionsThrottled
            }
        }
    }
}
