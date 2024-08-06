// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.Tool.UnitTests.CollectionRules.Triggers;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
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
    internal static class CollectionRulePipelineTestsHelper
    {
        internal static async Task ExecuteScenario(
            TargetFrameworkMoniker tfm,
            string scenarioName,
            string collectionRuleName,
            Action<Tools.Monitor.RootOptions> setup,
            Func<AppRunner, CollectionRulePipeline, PipelineCallbacks, Task> pipelineCallback,
            ITestOutputHelper outputHelper,
            Action<IServiceCollection> servicesCallback = null)
        {
            EndpointInfoSourceCallback endpointInfoCallback = new(outputHelper);
            EndpointUtilities endpointUtilities = new(outputHelper);
            await using ServerSourceHolder sourceHolder = await endpointUtilities.StartServerAsync(endpointInfoCallback);

            await using AppRunner runner = new(outputHelper, Assembly.GetExecutingAssembly(), tfm: tfm);
            runner.ConnectionMode = DiagnosticPortConnectionMode.Connect;
            runner.DiagnosticPortPath = sourceHolder.TransportName;
            runner.ScenarioName = scenarioName;

            Task<IProcessInfo> processInfoTask = endpointInfoCallback.WaitAddedProcessInfoAsync(runner, CommonTestTimeouts.StartProcess);

            await runner.ExecuteAsync(async () =>
            {
                IProcessInfo processInfo = await processInfoTask;

                await TestHostHelper.CreateCollectionRulesHost(
                    outputHelper,
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
                        TimeProvider timeProvider =
                            host.Services.GetRequiredService<TimeProvider>();

                        PipelineCallbacks callbacks = new();

                        CollectionRuleContext context = new(
                            collectionRuleName,
                            optionsMonitor.Get(collectionRuleName),
                            processInfo,
                            HostInfo.GetCurrent(timeProvider),
                            logger,
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

        /// <summary>
        /// Manually trigger for a number of iterations (<paramref name="iterationCount"/>) and test
        /// that the actions are invoked for the number of expected iterations (<paramref name="expectedCount"/>) and
        /// are throttled for the remaining number of iterations.
        /// </summary>
        internal static async Task ManualTriggerAsync(
            ManualTriggerService triggerService,
            CallbackActionService callbackService,
            PipelineCallbacks callbacks,
            int iterationCount,
            int expectedCount,
            MockTimeProvider timeProvider,
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
                timeProvider.Increment(clockIncrementDuration);
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
                timeProvider.Increment(clockIncrementDuration);
            }

            // Check that no actions have been executed.
            Assert.False(actionStartedTask.IsCompleted);
        }

        internal sealed class PipelineCallbacks
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

            private sealed class CompletionEntry
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
