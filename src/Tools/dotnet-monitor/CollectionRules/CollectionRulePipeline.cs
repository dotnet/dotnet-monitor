// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Triggers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules
{
    internal class CollectionRulePipeline : Pipeline
    {
        // The executor of the action list for the collection rule.
        private readonly ActionListExecutor _actionListExecutor;

        internal readonly CollectionRuleContext _context;

        // Task completion source for signalling when the pipeline has finished starting.
        private readonly Action _startCallback;

        // Operations for getting trigger information.
        private readonly ICollectionRuleTriggerOperations _triggerOperations;

        // Flag used to guard against multiple invocations of _startCallback.
        private bool _invokedStartCallback = false;

        internal CollectionRuleCleanupExplanation _cleanupExplanation;

        // Exposed for showing Collection Rule State
        internal Queue<DateTime> _executionTimestamps;
        internal List<DateTime> _allExecutionTimestamps = new();
        internal DateTime _pipelineStartTime;

        CollectionRulesStateUpdater stateHolder = new();

        public CollectionRulePipeline(
            ActionListExecutor actionListExecutor,
            ICollectionRuleTriggerOperations triggerOperations,
            CollectionRuleContext context,
            Action startCallback)
        {
            _actionListExecutor = actionListExecutor ?? throw new ArgumentNullException(nameof(actionListExecutor));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _startCallback = startCallback;
            _triggerOperations = triggerOperations ?? throw new ArgumentNullException(nameof(triggerOperations));
        }

        /// <summary>
        /// Runs the pipeline to completion.
        /// </summary>
        /// <remarks>
        /// The pipeline will only successfully complete in the following scenarios:
        /// (1) the trigger is a startup trigger and the action list successfully executes once.
        /// (2) without a specified action count window duration, the number of action list executions equals the action count limit.
        /// </remarks>
        protected override async Task OnRun(CancellationToken token)
        {
            if (!_triggerOperations.TryCreateFactory(_context.Options.Trigger.Type, out ICollectionRuleTriggerFactoryProxy factory))
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, Strings.ErrorMessage_CouldNotMapToTrigger, _context.Options.Trigger.Type));
            }

            using CancellationTokenSource durationCancellationSource = new();
            using CancellationTokenSource linkedCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(
                durationCancellationSource.Token,
                token);

            CancellationToken linkedToken = linkedCancellationSource.Token;

            TimeSpan? actionCountWindowDuration = _context.Options.Limits?.ActionCountSlidingWindowDuration;
            int actionCountLimit = (_context.Options.Limits?.ActionCount).GetValueOrDefault(CollectionRuleLimitsOptionsDefaults.ActionCount);
            _executionTimestamps = new(actionCountLimit);
            _allExecutionTimestamps = new();
            _pipelineStartTime = _context.Clock.UtcNow.UtcDateTime;

            // Start cancellation timer for graceful stop of the collection rule
            // when the rule duration has been specified. Conditionally enable this
            // based on if the rule has a duration limit.
            TimeSpan? ruleDuration = _context.Options.Limits?.RuleDuration;
            if (ruleDuration.HasValue)
            {
                durationCancellationSource.CancelAfter(ruleDuration.Value);
            }

            try
            {
                bool completePipeline = false;
                while (!completePipeline)
                {
                    TaskCompletionSource<object> triggerSatisfiedSource =
                        new(TaskCreationOptions.RunContinuationsAsynchronously);

                    ICollectionRuleTrigger trigger = null;
                    try
                    {
                        KeyValueLogScope triggerScope = new();
                        triggerScope.AddCollectionRuleTrigger(_context.Options.Trigger.Type);
                        IDisposable triggerScopeRegistration = _context.Logger.BeginScope(triggerScope);

                        _context.Logger.CollectionRuleTriggerStarted(_context.Name, _context.Options.Trigger.Type);

                        trigger = factory.Create(
                            _context.EndpointInfo,
                            () => triggerSatisfiedSource.TrySetResult(null),
                            _context.Options.Trigger.Settings);

                        if (null == trigger)
                        {
                            throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, Strings.ErrorMessage_TriggerFactoryFailed, _context.Options.Trigger.Type));
                        }

                        // Start the trigger.
                        await trigger.StartAsync(linkedToken).ConfigureAwait(false);

                        // The pipeline signals that it has started just after starting a non-startup trigger.
                        // Instances with startup triggers signal start after having finished executing the action list.
                        if (trigger is not ICollectionRuleStartupTrigger)
                        {
                            // Signal that the pipeline trigger is initialized.
                            InvokeStartCallback();
                        }

                        // Wait for the trigger to be satisfied.
                        await triggerSatisfiedSource.WithCancellation(linkedToken).ConfigureAwait(false);

                        _context.Logger.CollectionRuleTriggerCompleted(_context.Name, _context.Options.Trigger.Type);
                    }
                    finally
                    {
                        try
                        {
                            // Intentionally not using the linkedToken. If the linkedToken was signaled
                            // due to pipeline duration expiring, try to stop the trigger gracefully
                            // unless forced by a caller to the pipeline.
                            await trigger.StopAsync(token).ConfigureAwait(false);
                        }
                        finally
                        {
                            await DisposableHelper.DisposeAsync(trigger);
                        }
                    }

                    DateTime currentTimestamp = _context.Clock.UtcNow.UtcDateTime;

                    // Think about how we can roll this together with the following check - so that the function can dequeue and update our state
                    DequeueOldTimestamps(_executionTimestamps, actionCountWindowDuration, currentTimestamp);

                    // Check if executing actions has been throttled due to count limit
                    if (!ActionCountReached(actionCountLimit, _executionTimestamps.Count))
                    {
                        _executionTimestamps.Enqueue(currentTimestamp);
                        _allExecutionTimestamps.Add(currentTimestamp);

                        bool actionsCompleted = false;
                        try
                        {
                            stateHolder.BeginActionExecution();

                            // Intentionally not using the linkedToken. Allow the action list to execute gracefully
                            // unless forced by a caller to cancel or stop the running of the pipeline.
                            await _actionListExecutor.ExecuteActions(_context, InvokeStartCallback, token);

                            actionsCompleted = true;
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            // Bad action execution shouldn't fail the pipeline.
                            // Logging is already done by executor.
                        }
                        finally
                        {
                            if (!actionsCompleted)
                            {
                                stateHolder.ActionExecutionFailed();
                            }

                            // The collection rule has executed the action list the maximum
                            // number of times as specified by the limits and the action count
                            // window was not specified. Since the pipeline can no longer execute
                            // actions, the pipeline can complete.
                            completePipeline = actionCountLimit <= _executionTimestamps.Count &&
                                !actionCountWindowDuration.HasValue;
                        }

                        if (actionsCompleted)
                        {
                            stateHolder.ActionExecutionSucceeded();

                            _context.Logger.CollectionRuleActionsCompleted(_context.Name);
                        }
                    }
                    else
                    {
                        _context.ThrottledCallback?.Invoke();

                        _context.Logger.CollectionRuleThrottled(_context.Name);
                    }

                    linkedToken.ThrowIfCancellationRequested();

                    // If the trigger is a startup trigger, only execute the action list once
                    // and then complete the pipeline.
                    if (trigger is ICollectionRuleStartupTrigger)
                    {
                        // Signal that the pipeline trigger is initialized.
                        InvokeStartCallback();

                        // Complete the pipeline since the action list is only executed once
                        // for collection rules with startup triggers.
                        completePipeline = true;

                        stateHolder.StartupTriggerCompleted();
                    }
                }
            }
            catch (OperationCanceledException) when (durationCancellationSource.IsCancellationRequested)
            {
                // This exception is caused by the pipeline duration expiring.
                // Handle it to allow pipeline to be in completed state.
                stateHolder.RuleDurationReached();
            }
        }

        internal bool ActionCountReached(int actionCountLimit, int currExecutions)
        {
            if (actionCountLimit > currExecutions)
            {
                stateHolder.EndThrottled();
                return false;
            }
            else
            {
                stateHolder.BeginThrottled();
                return true;
            }
        }

        public static void DequeueOldTimestamps(Queue<DateTime> timestamps, TimeSpan? actionCountWindowDuration, DateTime currentTimestamp)
        {
            // If rule has an action count window, Remove all execution timestamps that fall outside the window.
            if (actionCountWindowDuration.HasValue)
            {
                DateTime windowStartTimestamp = currentTimestamp - actionCountWindowDuration.Value;
                while (timestamps.Count > 0)
                {
                    DateTime executionTimestamp = timestamps.Peek();
                    if (executionTimestamp < windowStartTimestamp)
                    {
                        timestamps.Dequeue();
                    }
                    else
                    {
                        // Stop clearing out previous executions
                        break;
                    }
                }
            }
        }

        // Temporary until Pipeline APIs are public or get an InternalsVisibleTo for the tests
        public new Task RunAsync(CancellationToken token)
        {
            return base.RunAsync(token);
        }

        // Temporary until Pipeline APIs are public or get an InternalsVisibleTo for the tests
        public new Task StopAsync(CancellationToken token)
        {
            return base.StopAsync(token);
        }

        // Ensures that the start callback is only invoked once.
        private void InvokeStartCallback()
        {
            if (!_invokedStartCallback)
            {
                _startCallback?.Invoke();
                _invokedStartCallback = true;
            }
        }

        protected override async Task OnCleanup()
        {
            TimeSpan? ruleDuration = _context.Options.Limits?.RuleDuration;

            if (ruleDuration.HasValue && _context.Clock.UtcNow.UtcDateTime - (_pipelineStartTime + ruleDuration.Value) > TimeSpan.Zero)
            {
                _cleanupExplanation = CollectionRuleCleanupExplanation.RuleDurationExceeded;
            }
            else
            {
                _cleanupExplanation = CollectionRuleCleanupExplanation.ConfigurationChanged;
            }

            await base.OnCleanup();
        }
    }
}
