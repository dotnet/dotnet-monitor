// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        public DateTime PipelineStartTime { get; private set; }

        // By locking here, the caller isn't forced to remember to lock when updating the state.
        // Locking here means that we will lock unnecessarily on the copy of the state; however,
        // given the scale of API calls, this should not be a performance issue.
        private readonly object _lock = new object();

        public CollectionRulePipelineState(CollectionRulePipelineState other)
        {
            // Gets a deep copy of the CollectionRulePipelineState
            lock (other._lock)
            {
                ActionCountLimit = other.ActionCountLimit;
                ActionCountSlidingWindowDuration = other.ActionCountSlidingWindowDuration;
                RuleDuration = other.RuleDuration;
                ExecutionTimestamps = new Queue<DateTime>(other.ExecutionTimestamps);
                AllExecutionTimestamps = new List<DateTime>(other.AllExecutionTimestamps);
                PipelineStartTime = other.PipelineStartTime;
                CurrentState = other.CurrentState;
                CurrentStateReason = other.CurrentStateReason;
            }
        }

        public CollectionRulePipelineState(int actionCountLimit, TimeSpan? actionCountSlidingWindowDuration, TimeSpan? ruleDuration, DateTime pipelineStartTime)
        {
            ActionCountLimit = actionCountLimit;
            ActionCountSlidingWindowDuration = actionCountSlidingWindowDuration;
            RuleDuration = ruleDuration;
            ExecutionTimestamps = new Queue<DateTime>(ActionCountLimit);
            AllExecutionTimestamps = new List<DateTime>();
            PipelineStartTime = pipelineStartTime;
            CurrentState = CollectionRuleState.Running;
            CurrentStateReason = Strings.Message_CollectionRuleStateReason_Running;
        }

        public bool BeginActionExecution(DateTime currentTime)
        {
            if (!CheckForThrottling(currentTime))
            {
                lock (_lock)
                {
                    Debug.Assert(CurrentState != CollectionRuleState.Finished);

                    ExecutionTimestamps.Enqueue(currentTime);
                    AllExecutionTimestamps.Add(currentTime);

                    CurrentState = CollectionRuleState.ActionExecuting;
                    CurrentStateReason = Strings.Message_CollectionRuleStateReason_ExecutingActions;
                }

                return true;
            }

            return false;
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

        public void CollectionRuleFinished(CollectionRuleFinishedStates finishedState)
        {
            string finishedStateReason = "";

            switch (finishedState)
            {
                case CollectionRuleFinishedStates.Startup:
                    finishedStateReason = Strings.Message_CollectionRuleStateReason_Finished_Startup;
                    break;
                case CollectionRuleFinishedStates.ActionCountReached:
                    finishedStateReason = Strings.Message_CollectionRuleStateReason_Finished_ActionCount;
                    break;
                case CollectionRuleFinishedStates.RuleDurationReached:
                    finishedStateReason = Strings.Message_CollectionRuleStateReason_Finished_RuleDuration;
                    break;
            }

            lock (_lock)
            {
                Debug.Assert(CurrentState != CollectionRuleState.Finished);

                CurrentState = CollectionRuleState.Finished;
                CurrentStateReason = finishedStateReason;
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

        public bool CheckForThrottling(DateTime currentTime)
        {
            bool isThrottled;

            lock (_lock)
            {
                DequeueOldTimestamps(ExecutionTimestamps, ActionCountSlidingWindowDuration, currentTime);
                isThrottled = CheckForThrottling(ActionCountLimit, ActionCountSlidingWindowDuration, ExecutionTimestamps.Count);
            }

            if (!isThrottled)
            {
                EndThrottled();
            }
            else
            {
                BeginThrottled();
            }

            return isThrottled;
        }

        public bool ActionExecutionCompleted(bool success)
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

        public bool CheckForActionCountLimitReached()
        {
            bool limitReached;

            lock (_lock)
            {
                limitReached = ActionCountLimit <= ExecutionTimestamps.Count && !ActionCountSlidingWindowDuration.HasValue;
            }

            if (limitReached)
            {
                CollectionRuleFinished(CollectionRuleFinishedStates.ActionCountReached);
            }

            return limitReached;
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
