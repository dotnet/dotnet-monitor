// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal class CollectionRuleStateHolder
    {
        public CollectionRuleState CurrState { get; private set; } = CollectionRuleState.Running;
        public string CurrStateReason { get; private set; } = CollectionRuleStateReasons.Running;

        internal void BeginActionExecution()
        {
            CurrState = CollectionRuleState.ActionExecuting;
            CurrStateReason = CollectionRuleStateReasons.ExecutingActions;
        }

        internal void ActionExecutionSucceeded()
        {
            CurrState = CollectionRuleState.Running;
            CurrStateReason = CollectionRuleStateReasons.Running;
        }


        // NOT SURE WHAT TO DO HERE
        internal void ActionExecutionFailed()
        {

        }

        internal void BeginThrottled()
        {
            CurrState = CollectionRuleState.Throttled;
            CurrStateReason = CollectionRuleStateReasons.Throttled;
        }

        internal void EndThrottled()
        {
            if (CurrState == CollectionRuleState.Throttled)
            {
                CurrState = CollectionRuleState.Running;
                CurrStateReason = CollectionRuleStateReasons.Running;
            }
        }

        internal void StartupTriggerCompleted()
        {
            CurrState = CollectionRuleState.Finished;
            CurrStateReason = CollectionRuleStateReasons.Finished_Startup;
        }

        internal void RuleDurationReached()
        {
            CurrState = CollectionRuleState.Finished;
            CurrStateReason = CollectionRuleStateReasons.Finished_RuleDuration;
        }

        internal void ActionCountReached()
        {
            CurrState = CollectionRuleState.Finished;
            CurrStateReason = CollectionRuleStateReasons.Finished_ActionCount;
        }

        internal void ConfigurationChanged()
        {
            CurrState = CollectionRuleState.Finished;
            CurrStateReason = CollectionRuleStateReasons.Finished_ConfigurationChanged;
        }

        // Not sure what the use-case is here (if there is one)
        internal void RuleFailure()
        {
            CurrState = CollectionRuleState.Finished;
            CurrStateReason = CollectionRuleStateReasons.Finished_Failure;
        }
    }
}
