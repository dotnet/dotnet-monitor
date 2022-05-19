// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal class CollectionRuleStateHolder
    {
        public CollectionRuleState CurrentState { get; private set; } = CollectionRuleState.Running;
        public string CurrentStateReason { get; private set; } = Strings.Message_CollectionRuleStateReason_Running;

        public CollectionRuleStateHolder(CollectionRuleStateHolder other)
        {
            CurrentState = other.CurrentState;
            CurrentStateReason = other.CurrentStateReason;
        }

        public CollectionRuleStateHolder()
        {
            CurrentState = CollectionRuleState.Running;
            CurrentStateReason = Strings.Message_CollectionRuleStateReason_Running;
        }

        public void BeginActionExecution()
        {
            CurrentState = CollectionRuleState.ActionExecuting;
            CurrentStateReason = Strings.Message_CollectionRuleStateReason_ExecutingActions;
        }

        public void ActionExecutionSucceeded()
        {
            CurrentState = CollectionRuleState.Running;
            CurrentStateReason = Strings.Message_CollectionRuleStateReason_Running;
        }

        public void ActionExecutionFailed()
        {
            // Is this the correct behavior? Treating the same as action success, but internally store this separately if we want to handle it differently
            CurrentState = CollectionRuleState.Running;
            CurrentStateReason = Strings.Message_CollectionRuleStateReason_Running;
        }

        public void BeginThrottled()
        {
            CurrentState = CollectionRuleState.Throttled;
            CurrentStateReason = Strings.Message_CollectionRuleStateReason_Throttled;
        }

        public void EndThrottled()
        {
            if (CurrentState == CollectionRuleState.Throttled)
            {
                CurrentState = CollectionRuleState.Running;
                CurrentStateReason = Strings.Message_CollectionRuleStateReason_Running;
            }
        }

        public void StartupTriggerCompleted()
        {
            CurrentState = CollectionRuleState.Finished;
            CurrentStateReason = Strings.Message_CollectionRuleStateReason_Finished_Startup;
        }

        public void RuleDurationReached()
        {
            CurrentState = CollectionRuleState.Finished;
            CurrentStateReason = Strings.Message_CollectionRuleStateReason_Finished_RuleDuration;
        }

        public void ActionCountReached()
        {
            CurrentState = CollectionRuleState.Finished;
            CurrentStateReason = Strings.Message_CollectionRuleStateReason_Finished_ActionCount;
        }

        public void RuleFailure(string errorMessage)
        {
            CurrentState = CollectionRuleState.Finished;
            CurrentStateReason = string.Format(Strings.Message_CollectionRuleStateReason_Finished_Failure, errorMessage);
        }
    }
}
