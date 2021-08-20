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

        // The endpiont that represents the process on which the collection rule is executed.
        private readonly IEndpointInfo _endpointInfo;

        // The rule description that determines the behavior of the pipeline.
        private readonly CollectionRuleOptions _ruleOptions;
        
        // Task completion source for signalling when the pipeline has finished starting.
        private readonly TaskCompletionSource<object> _startedSource =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        
        // Operations for getting triger information.
        private readonly ICollectionRuleTriggerOperations _triggerOperations;

        public CollectionRulePipeline(
            ActionListExecutor actionListExecutor,
            ICollectionRuleTriggerOperations triggerOperations,
            CollectionRuleOptions ruleOptions,
            IEndpointInfo endpointInfo)
        {
            _actionListExecutor = actionListExecutor ?? throw new ArgumentNullException(nameof(actionListExecutor));
            _endpointInfo = endpointInfo ?? throw new ArgumentNullException(nameof(endpointInfo));
            _ruleOptions = ruleOptions ?? throw new ArgumentNullException(nameof(ruleOptions));
            _triggerOperations = triggerOperations ?? throw new ArgumentNullException(nameof(triggerOperations));
        }

        /// <summary>
        /// Starts the execution of the pipeline without waiting for it to run to completion.
        /// </summary>
        /// <remarks>
        /// If the specified trigger is a startup trigger, this method will complete when the
        /// action list has completed execution. If the specified trigger is not a startup
        /// trigger, this method will complete after the trigger has been started.
        /// </remarks>
        public async Task StartAsync(CancellationToken token)
        {
            var runTask = RunAsync(token);

            await _startedSource.WithCancellation(token).ConfigureAwait(false);
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
            if (!_triggerOperations.TryCreateFactory(_ruleOptions.Trigger.Type, out ICollectionRuleTriggerFactoryProxy factory))
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, Strings.ErrorMessage_CouldNotMapToTrigger, _ruleOptions.Trigger.Type));
            }

            using CancellationTokenSource durationCancellationSource = new();
            using CancellationTokenSource linkedCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(
                durationCancellationSource.Token,
                token);

            CancellationToken linkedToken = linkedCancellationSource.Token;

            TimeSpan? actionCountWindowDuration = _ruleOptions.Limits?.ActionCountSlidingWindowDuration;
            int actionCountLimit = (_ruleOptions.Limits?.ActionCount).GetValueOrDefault(CollectionRuleLimitsOptionsDefaults.ActionCount);
            Queue<DateTime> executionTimestamps = new(actionCountLimit);

            // Start cancellation timer for graceful stop of the collection rule
            // when the rule duration has been specified. Conditionally enable this
            // based on if the rule has a duration limit.
            TimeSpan? ruleDuration = _ruleOptions.Limits?.RuleDuration;
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
                        trigger = factory.Create(
                            _endpointInfo,
                            () => triggerSatisfiedSource.TrySetResult(null),
                            _ruleOptions.Trigger.Settings);

                        if (null == trigger)
                        {
                            throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, Strings.ErrorMessage_TriggerFactoryFailed, _ruleOptions.Trigger.Type));
                        }

                        // Start the trigger.
                        await trigger.StartAsync(linkedToken).ConfigureAwait(false);

                        // The pipeline signals that it has started just after starting a non-startup trigger.
                        // Instances with startup triggers signal start after having finished executing the action list.
                        if (trigger is not ICollectionRuleStartupTrigger)
                        {
                            // Signal that the pipeline trigger is initialized.
                            _startedSource.TrySetResult(null);
                        }

                        // Wait for the trigger to be satisfied.
                        await triggerSatisfiedSource.WithCancellation(linkedToken).ConfigureAwait(false);
                    }
                    finally
                    {
                        try
                        {
                            // Intentially not using the linkedToken. If the linkedToken was signaled
                            // due to pipeline duration expiring, try to stop the trigger gracefully
                            // unless forced by a caller to the pipeline.
                            await trigger.StopAsync(token).ConfigureAwait(false);
                        }
                        finally
                        {
                            if (trigger is IAsyncDisposable asyncDisposableTrigger)
                            {
                                await asyncDisposableTrigger.DisposeAsync().ConfigureAwait(false);
                            }
                            else if (trigger is IDisposable disposableTrigger)
                            {
                                disposableTrigger.Dispose();
                            }
                        }
                    }

                    DateTime currentTimestamp = DateTime.Now;

                    // If rule has an action count window, Remove all execution timestamps that fall outside the window.
                    if (actionCountWindowDuration.HasValue)
                    {
                        DateTime windowStartTimestamp = currentTimestamp - actionCountWindowDuration.Value;
                        while (executionTimestamps.Count > 0)
                        {
                            DateTime executionTimestamp = executionTimestamps.Peek();
                            if (executionTimestamp < windowStartTimestamp)
                            {
                                executionTimestamps.Dequeue();
                            }
                            else
                            {
                                // Stop clearing out previous executions
                                break;
                            }
                        }
                    }

                    // Check if executing actions has been throttled due to count limit
                    if (actionCountLimit > executionTimestamps.Count)
                    {
                        executionTimestamps.Enqueue(currentTimestamp);

                        try
                        {
                            // Intentionally not using the linkedToken. Allow the action list to execute gracefully
                            // unless forced by a caller to cancel or stop the running of the pipeline.
                            await _actionListExecutor.ExecuteActions(_ruleOptions.Actions, _endpointInfo, token);
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            // Bad action execution shouldn't fail the pipeline.
                            // TODO: log that the action execution has failed.
                        }
                        finally
                        {
                            // The collection rule has executed the action list the maximum
                            // number of times as specified by the limits and the action count
                            // window was not specified. Since the pipeline can no longer execute
                            // actions, the pipeline can complete.
                            completePipeline = actionCountLimit <= executionTimestamps.Count &&
                                !actionCountWindowDuration.HasValue;
                        }
                    }
                    else
                    {
                        // Throttled
                    }

                    linkedToken.ThrowIfCancellationRequested();

                    // If the trigger is a startup trigger, only execute the action list once
                    // and then complete the pipeline.
                    if (trigger is ICollectionRuleStartupTrigger)
                    {
                        // Signal that the pipeline trigger is initialized.
                        _startedSource.TrySetResult(null);

                        // Complete the pipeline since the action list is only executed once
                        // for collection rules with startup triggers.
                        completePipeline = true;
                    }
                }
            }
            catch (OperationCanceledException) when (durationCancellationSource.IsCancellationRequested)
            {
                // This exception is caused by the pipeline duration expiring.
                // Handle it to allow pipeline to be in completed state.
            }
        }

        protected override Task OnCleanup()
        {
            _startedSource.TrySetCanceled();

            return base.OnCleanup();
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
    }
}
