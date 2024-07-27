// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Triggers;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules
{
    internal class CollectionRulePipeline : Pipeline
    {
        // The executor of the action list for the collection rule.
        private readonly ActionListExecutor _actionListExecutor;

        public CollectionRuleContext Context { get; }

        // Task completion source for signalling when the pipeline has finished starting.
        private readonly Action _startCallback;

        // Operations for getting trigger information.
        private readonly ICollectionRuleTriggerOperations _triggerOperations;

        // Flag used to guard against multiple invocations of _startCallback.
        private bool _invokedStartCallback;

#nullable disable
        private CollectionRulePipelineState _stateHolder;
#nullable restore

        public CollectionRulePipeline(
            ActionListExecutor actionListExecutor,
            ICollectionRuleTriggerOperations triggerOperations,
            CollectionRuleContext context,
            Action startCallback)
        {
            _actionListExecutor = actionListExecutor ?? throw new ArgumentNullException(nameof(actionListExecutor));
            Context = context ?? throw new ArgumentNullException(nameof(context));
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
            if (!_triggerOperations.TryCreateFactory(Context.Options.Trigger.Type, out ICollectionRuleTriggerFactoryProxy factory))
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, Strings.ErrorMessage_CouldNotMapToTrigger, Context.Options.Trigger.Type));
            }

            using CancellationTokenSource durationCancellationSource = new();
            using CancellationTokenSource linkedCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(
                durationCancellationSource.Token,
                token);

            CancellationToken linkedToken = linkedCancellationSource.Token;

            _stateHolder = new CollectionRulePipelineState(
                (Context.Options.Limits?.ActionCount).GetValueOrDefault(CollectionRuleLimitsOptionsDefaults.ActionCount),
                Context.Options.Limits?.ActionCountSlidingWindowDuration,
                Context.Options.Limits?.RuleDuration,
                Context.HostInfo.TimeProvider.GetUtcNow().UtcDateTime);

            // Start cancellation timer for graceful stop of the collection rule
            // when the rule duration has been specified. Conditionally enable this
            // based on if the rule has a duration limit.
            if (_stateHolder.RuleDuration.HasValue)
            {
                durationCancellationSource.CancelAfter(_stateHolder.RuleDuration.Value);
            }

            try
            {
                bool completePipeline = false;
                while (!completePipeline)
                {
                    TaskCompletionSource<object?> triggerSatisfiedSource =
                        new(TaskCreationOptions.RunContinuationsAsynchronously);

                    ICollectionRuleTrigger? trigger = null;
                    try
                    {
                        KeyValueLogScope triggerScope = new();
                        triggerScope.AddCollectionRuleTrigger(Context.Options.Trigger.Type);
                        using IDisposable? triggerScopeRegistration = Context.Logger.BeginScope(triggerScope);

                        Context.Logger.CollectionRuleTriggerStarted(Context.Name, Context.Options.Trigger.Type);

                        trigger = factory.Create(
                            Context.EndpointInfo,
                            () => triggerSatisfiedSource.TrySetResult(null),
                            Context.Options.Trigger.Settings);

                        if (null == trigger)
                        {
                            throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, Strings.ErrorMessage_TriggerFactoryFailed, Context.Options.Trigger.Type));
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

                        Context.Logger.CollectionRuleTriggerCompleted(Context.Name, Context.Options.Trigger.Type);
                    }
                    finally
                    {
                        if (trigger != null)
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

                    }

                    DateTime currentTimestamp = Context.HostInfo.TimeProvider.GetUtcNow().UtcDateTime;

                    if (_stateHolder.BeginActionExecution(currentTimestamp))
                    {
                        bool actionsCompleted = false;
                        try
                        {
                            // Intentionally not using the linkedToken. Allow the action list to execute gracefully
                            // unless forced by a caller to cancel or stop the running of the pipeline.
                            await _actionListExecutor.ExecuteActions(Context, InvokeStartCallback, token);

                            actionsCompleted = true;
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            // Bad action execution shouldn't fail the pipeline.
                            // Logging is already done by executor.
                        }
                        finally
                        {
                            if (_stateHolder.ActionExecutionCompleted(actionsCompleted))
                            {
                                Context.Logger.CollectionRuleActionsCompleted(Context.Name);
                            }

                            // The collection rule has executed the action list the maximum
                            // number of times as specified by the limits and the action count
                            // window was not specified. Since the pipeline can no longer execute
                            // actions, the pipeline can complete.

                            completePipeline = _stateHolder.CheckForActionCountLimitReached();
                        }
                    }
                    else
                    {
                        Context.ThrottledCallback?.Invoke();

                        Context.Logger.CollectionRuleThrottled(Context.Name);
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

                        _stateHolder.CollectionRuleFinished(CollectionRuleFinishedStates.Startup);
                    }
                }
            }
            catch (OperationCanceledException) when (durationCancellationSource.IsCancellationRequested)
            {
                // This exception is caused by the pipeline duration expiring.
                // Handle it to allow pipeline to be in completed state.
                _stateHolder.CollectionRuleFinished(CollectionRuleFinishedStates.RuleDurationReached);
            }
            catch (Exception ex)
            {
                _stateHolder.RuleFailure(ex.Message);
                throw;
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

        public CollectionRulePipelineState GetPipelineState()
        {
            CollectionRulePipelineState pipelineStateCopy = new CollectionRulePipelineState(_stateHolder);

            _ = pipelineStateCopy.CheckForThrottling(Context.HostInfo.TimeProvider.GetUtcNow().UtcDateTime);

            return pipelineStateCopy;
        }
    }
}
