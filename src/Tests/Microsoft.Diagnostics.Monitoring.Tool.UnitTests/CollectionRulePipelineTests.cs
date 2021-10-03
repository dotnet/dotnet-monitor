﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.Diagnostics.Monitoring.TestCommon.Options;
using Microsoft.Diagnostics.Monitoring.TestCommon.Runners;
using Microsoft.Diagnostics.Monitoring.Tool.UnitTests.CollectionRules.Actions;
using Microsoft.Diagnostics.Monitoring.Tool.UnitTests.CollectionRules.Triggers;
using Microsoft.Diagnostics.Monitoring.WebApi;
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
                async (runner, pipeline, startedTask) =>
                {
                    using CancellationTokenSource cancellationSource = new(DefaultPipelineTimeout);

                    // Register first callback before pipeline starts. This callback should be completed before
                    // the pipeline finishes starting.
                    Task callback1Task = await callbackService.StartWaitForCallbackAsync(cancellationSource.Token);

                    // Startup trigger will cause the the pipeline to complete the start phase
                    // after the action list has been completed.
                    Task runTask = pipeline.RunAsync(cancellationSource.Token);

                    await startedTask.WithCancellation(cancellationSource.Token);

                    // Register second callback after pipeline starts. The second callback should not be
                    // completed because it was registered after the pipeline had finished starting. Since
                    // the action list is only ever executed once and is executed before the pipeline finishes
                    // starting, thus subsequent invocations of the action list should not occur.
                    Task callback2Task = await callbackService.StartWaitForCallbackAsync(cancellationSource.Token);

                    // Since the action list was completed before the pipeline finished starting,
                    // the action should have invoked it's callback.
                    await callback1Task.WithCancellation(cancellationSource.Token);

                    // Regardless of the action list constraints, the pipeline should have only
                    // executed the action list once due to the use of a startup trigger.
                    VerifyExecutionCount(callbackService, 1);

                    // Validate that the action list was not executed a second time.
                    Assert.False(callback2Task.IsCompletedSuccessfully);

                    // Pipeline should have completed shortly after finished starting. This should only
                    // wait for a very short time, if at all.
                    await runTask.WithCancellation(cancellationSource.Token);

                    // Validate that the action list was not executed a second time.
                    Assert.False(callback2Task.IsCompletedSuccessfully);

                    await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);
                },
                services =>
                {
                    services.RegisterTestAction(callbackService);
                });
        }

        /// <summary>
        /// Test that the pipeline works with the EventCounter trigger.
        /// </summary>
        [Theory]
        [MemberData(nameof(GetTfmsSupportingPortListener))]
        public Task CollectionRulePipeline_EventCounterTriggerTest(TargetFrameworkMoniker appTfm)
        {
            CallbackActionService callbackService = new(_outputHelper);

            return ExecuteScenario(
                appTfm,
                TestAppScenarios.SpinWait.Name,
                TestRuleName,
                options =>
                {
                    options.CreateCollectionRule(TestRuleName)
                        .SetEventCounterTrigger(out EventCounterOptions eventCounterOptions)
                        .AddAction(CallbackAction.ActionName);

                    // cpu usage greater that 5% for 2 seconds
                    eventCounterOptions.ProviderName = "System.Runtime";
                    eventCounterOptions.CounterName = "cpu-usage";
                    eventCounterOptions.GreaterThan = 5;
                    eventCounterOptions.SlidingWindowDuration = TimeSpan.FromSeconds(2);
                },
                async (runner, pipeline, startedTask) =>
                {
                    using CancellationTokenSource cancellationSource = new(DefaultPipelineTimeout);

                    // Register first callback before pipeline starts. This callback should be completed after
                    // the pipeline finishes starting.
                    Task callbackTask = await callbackService.StartWaitForCallbackAsync(cancellationSource.Token);

                    // Start pipeline with EventCounter trigger.
                    Task runTask = pipeline.RunAsync(cancellationSource.Token);

                    await startedTask.WithCancellation(cancellationSource.Token);

                    await runner.SendCommandAsync(TestAppScenarios.SpinWait.Commands.StartSpin);

                    // This should not complete until the trigger conditions are satisfied for the first time.
                    await callbackTask.WithCancellation(cancellationSource.Token);

                    VerifyExecutionCount(callbackService, 1);

                    await runner.SendCommandAsync(TestAppScenarios.SpinWait.Commands.StopSpin);

                    // Validate that the pipeline is not in a completed state.
                    // The pipeline should already be running since it was started.
                    Assert.False(runTask.IsCompleted);

                    await pipeline.StopAsync(cancellationSource.Token);
                },
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
                },
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
                        .SetActionLimits(count: ExpectedActionExecutionCount);
                },
                async (runner, pipeline, startedTask) =>
                {
                    using CancellationTokenSource cancellationSource = new(DefaultPipelineTimeout);

                    Task runTask = pipeline.RunAsync(cancellationSource.Token);

                    await startedTask.WithCancellation(cancellationSource.Token);

                    await ManualTriggerBurstAsync(triggerService);

                    // Pipeline should run to completion due to action count limit without sliding window.
                    await runTask.WithCancellation(cancellationSource.Token);

                    await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);

                    // Action list should have been executed the expected number of times
                    VerifyExecutionCount(callbackService, ExpectedActionExecutionCount);
                },
                services =>
                {
                    services.RegisterManualTrigger(triggerService);
                    services.RegisterTestAction(callbackService);
                });
        }

        /// <summary>
        /// Test that the CollectionRulePipeline thottles actions when action count limit is reached within window.
        /// </summary>
        [Theory]
        [MemberData(nameof(GetTfmsSupportingPortListener))]
        public Task CollectionRulePipeline_ActionCountLimitSlidingDurationTest(TargetFrameworkMoniker appTfm)
        {
            const int ExpectedActionExecutionCount = 3;
            TimeSpan SlidingWindowDuration = TimeSpan.FromSeconds(5);

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
                        .SetActionLimits(
                            count: ExpectedActionExecutionCount,
                            slidingWindowDuration: SlidingWindowDuration);
                },
                async (runner, pipeline, startedTask) =>
                {
                    using CancellationTokenSource cancellationSource = new(DefaultPipelineTimeout);

                    Task runTask = pipeline.RunAsync(cancellationSource.Token);

                    await startedTask.WithCancellation(cancellationSource.Token);

                    await ManualTriggerBurstAsync(triggerService, count: 5);

                    // Action list should have been executed the expected number of times
                    VerifyExecutionCount(callbackService, ExpectedActionExecutionCount);

                    // Wait for existing execution times to fall out of sliding window.
                    await Task.Delay(SlidingWindowDuration * 1.2);

                    await ManualTriggerBurstAsync(triggerService, count: 5);

                    // Expect total action invocation count to be twice the limit
                    VerifyExecutionCount(callbackService, 2 * ExpectedActionExecutionCount);

                    // Pipeline should not run to completion due to sliding window existance.
                    Assert.False(runTask.IsCompleted);

                    await runner.SendCommandAsync(TestAppScenarios.AsyncWait.Commands.Continue);

                    await pipeline.StopAsync(cancellationSource.Token);
                },
                services =>
                {
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

        private async Task ManualTriggerBurstAsync(ManualTriggerService service, int count = 10)
        {
            for (int i = 0; i < count; i++)
            {
                service.NotifySubscribers();
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
            Func<AppRunner, CollectionRulePipeline, Task, Task> pipelineCallback,
            Action<IServiceCollection> servicesCallback = null)
        {
            DiagnosticPortHelper.Generate(DiagnosticPortConnectionMode.Listen, out _, out string transportName);
            _outputHelper.WriteLine("Starting server endpoint info source at '" + transportName + "'.");

            AppRunner runner = new(_outputHelper, Assembly.GetExecutingAssembly(), tfm: tfm);
            runner.ConnectionMode = DiagnosticPortConnectionMode.Connect;
            runner.DiagnosticPortPath = transportName;
            runner.ScenarioName = scenarioName;

            EndpointInfoSourceCallback endpointInfoCallback = new(_outputHelper);
            List<Tools.Monitor.IEndpointInfoSourceCallbacks> callbacks = new();
            callbacks.Add(endpointInfoCallback);
            Tools.Monitor.ServerEndpointInfoSource source = new(transportName, callbacks);
            source.Start();

            Task<IEndpointInfo> endpointInfoTask = endpointInfoCallback.WaitForNewEndpointInfoAsync(runner, CommonTestTimeouts.StartProcess);

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

                        CollectionRuleContext context = new(
                            collectionRuleName,
                            optionsMonitor.Get(collectionRuleName),
                            endpointInfo,
                            logger);

                        TaskCompletionSource<object> startedSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
                        int startedCount = 0;

                        await using CollectionRulePipeline pipeline = new(
                            actionListExecutor,
                            triggerOperations,
                            context,
                            () => { startedSource.TrySetResult(null); startedCount++; });

                        await pipelineCallback(runner, pipeline, startedSource.Task);

                        Assert.Equal(1, startedCount);
                    },
                    servicesCallback);
            });
        }
    }
}
