// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal class CollectionRulePipelineState
    {
        public CollectionRuleState CurrentState { get; private set; } = CollectionRuleState.Running;
        public string CurrentStateReason { get; private set; } = Strings.Message_CollectionRuleStateReason_Running;
        public Queue<DateTime> ExecutionTimestamps { get; set; }
        public List<DateTime> AllExecutionTimestamps { get; set; }
        public TimeSpan? ActionCountSlidingWindowDuration { get; private set; }
        public TimeSpan? RuleDuration { get; private set; }
        public int ActionCountLimit { get; private set; }

        private readonly object _lock = new object();

        public CollectionRulePipelineState(CollectionRulePipelineState other)
        {
            lock (other._lock)
            {
                ActionCountLimit = other.ActionCountLimit;
                ActionCountSlidingWindowDuration = other.ActionCountSlidingWindowDuration;
                RuleDuration = other.RuleDuration;
                ExecutionTimestamps = other.ExecutionTimestamps;
                AllExecutionTimestamps = other.AllExecutionTimestamps;
                CurrentState = other.CurrentState;
                CurrentStateReason = other.CurrentStateReason;
            }
        }

        public CollectionRulePipelineState(int actionCountLimit, TimeSpan? actionCountSlidingWindowDuration, TimeSpan? ruleDuration)
        {
            ActionCountLimit = actionCountLimit;
            ActionCountSlidingWindowDuration = actionCountSlidingWindowDuration;
            RuleDuration = ruleDuration;
            ExecutionTimestamps = new Queue<DateTime>(ActionCountLimit);
            AllExecutionTimestamps = new List<DateTime>();

            lock (_lock)
            {
                Debug.Assert(CurrentState != CollectionRuleState.Finished);

                CurrentState = CollectionRuleState.Running;
                CurrentStateReason = Strings.Message_CollectionRuleStateReason_Running;
            }
        }

        public void BeginActionExecution()
        {
            lock (_lock)
            {
                Debug.Assert(CurrentState != CollectionRuleState.Finished);

                CurrentState = CollectionRuleState.ActionExecuting;
                CurrentStateReason = Strings.Message_CollectionRuleStateReason_ExecutingActions;
            }
        }

        private void ActionExecutionSucceeded()
        {
            lock (_lock)
            {
                Debug.Assert(CurrentState == CollectionRuleState.ActionExecuting);

                CurrentState = CollectionRuleState.Running;
                CurrentStateReason = Strings.Message_CollectionRuleStateReason_Running;
            }
        }

        private void ActionExecutionFailed()
        {
            lock (_lock)
            {
                Debug.Assert(CurrentState == CollectionRuleState.ActionExecuting);

                CurrentState = CollectionRuleState.Running;
                CurrentStateReason = Strings.Message_CollectionRuleStateReason_Running;
            }
        }

        private void BeginThrottled()
        {
            lock (_lock)
            {
                Debug.Assert(CurrentState != CollectionRuleState.Finished);

                CurrentState = CollectionRuleState.Throttled;
                CurrentStateReason = Strings.Message_CollectionRuleStateReason_Throttled;
            }
        }

        private void EndThrottled()
        {
            lock (_lock)
            {
                if (CurrentState == CollectionRuleState.Throttled)
                {
                    CurrentState = CollectionRuleState.Running;
                    CurrentStateReason = Strings.Message_CollectionRuleStateReason_Running;
                }
            }
        }

        public void StartupTriggerCompleted()
        {
            lock (_lock)
            {
                Debug.Assert(CurrentState != CollectionRuleState.Finished);

                CurrentState = CollectionRuleState.Finished;
                CurrentStateReason = Strings.Message_CollectionRuleStateReason_Finished_Startup;
            }
        }

        public void RuleDurationReached()
        {
            lock (_lock)
            {
                Debug.Assert(CurrentState != CollectionRuleState.Finished);

                CurrentState = CollectionRuleState.Finished;
                CurrentStateReason = Strings.Message_CollectionRuleStateReason_Finished_RuleDuration;
            }
        }

        public void ActionCountReached()
        {
            lock (_lock)
            {
                CurrentState = CollectionRuleState.Finished;
                CurrentStateReason = Strings.Message_CollectionRuleStateReason_Finished_ActionCount;
            }
        }

        public void RuleFailure(string errorMessage)
        {
            lock (_lock)
            {
                Debug.Assert(CurrentState != CollectionRuleState.Finished);

                CurrentState = CollectionRuleState.Finished;
                CurrentStateReason = string.Format(CultureInfo.InvariantCulture, Strings.Message_CollectionRuleStateReason_Finished_Failure, errorMessage);
            }
        }

        public bool CanExecuteActions(DateTime currentTime)
        {
            lock (ExecutionTimestamps)
            {
                DequeueOldTimestamps(ExecutionTimestamps, ActionCountSlidingWindowDuration, currentTime);
            }
            bool canExecuteActions = !CheckForThrottling(ActionCountLimit, ActionCountSlidingWindowDuration, ExecutionTimestamps.Count);
        
            if (canExecuteActions)
            {
                EndThrottled();
            }
            else
            {
                BeginThrottled();
            }

            return canExecuteActions;
        }

        public bool ActionExecutionSucceeded(bool success)
        {
            if (!success)
            {
                ActionExecutionFailed();
            }
            else
            {
                ActionExecutionSucceeded();
            }

            return success;
        }

        private static void DequeueOldTimestamps(Queue<DateTime> executionTimestamps, TimeSpan? actionCountWindowDuration, DateTime currentTimestamp)
        {
            // If rule has an action count window, remove all execution timestamps that fall outside the window.
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
        }

        private static bool CheckForThrottling(int actionCountLimit, TimeSpan? actionCountSWD, int executionTimestampsCount)
        {
            return actionCountSWD.HasValue && actionCountLimit <= executionTimestampsCount;
        }
    }
}
