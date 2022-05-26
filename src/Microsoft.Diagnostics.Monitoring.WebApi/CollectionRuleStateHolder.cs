// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Collections.Generic;
using System;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal class CollectionRuleStateHolder
    {
        public CollectionRuleState CurrentState { get; private set; }
        public string CurrentStateReason { get; private set; }

        public Queue<DateTime> ExecutionTimestamps { get; private set; }

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
            // Is this the correct behavior?
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
            CurrentStateReason = string.Format(CultureInfo.InvariantCulture, Strings.Message_CollectionRuleStateReason_Finished_Failure, errorMessage);
        }
    }
}
