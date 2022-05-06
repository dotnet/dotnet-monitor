// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal class CollectionRuleStateHolder
    {
        public CollectionRuleState CurrState { get; private set; } = CollectionRuleState.Running;
        public string CurrStateReason { get; private set; } = Strings.Message_CollectionRuleStateReason_Running;

        public void BeginActionExecution()
        {
            CurrState = CollectionRuleState.ActionExecuting;
            CurrStateReason = Strings.Message_CollectionRuleStateReason_ExecutingActions;
        }

        public void ActionExecutionSucceeded()
        {
            CurrState = CollectionRuleState.Running;
            CurrStateReason = Strings.Message_CollectionRuleStateReason_Running;
        }

        public void ActionExecutionFailed()
        {
            // Is this the correct behavior? Treating the same as action success, but internally store this separately if we want to handle it differently
            CurrState = CollectionRuleState.Running;
            CurrStateReason = Strings.Message_CollectionRuleStateReason_Running;
        }

        public void BeginThrottled()
        {
            CurrState = CollectionRuleState.Throttled;
            CurrStateReason = Strings.Message_CollectionRuleStateReason_Throttled;
        }

        public void EndThrottled()
        {
            if (CurrState == CollectionRuleState.Throttled)
            {
                CurrState = CollectionRuleState.Running;
                CurrStateReason = Strings.Message_CollectionRuleStateReason_Running;
            }
        }

        public void StartupTriggerCompleted()
        {
            CurrState = CollectionRuleState.Finished;
            CurrStateReason = Strings.Message_CollectionRuleStateReason_Finished_Startup;
        }

        public void RuleDurationReached()
        {
            CurrState = CollectionRuleState.Finished;
            CurrStateReason = Strings.Message_CollectionRuleStateReason_Finished_RuleDuration;
        }

        public void ActionCountReached()
        {
            CurrState = CollectionRuleState.Finished;
            CurrStateReason = Strings.Message_CollectionRuleStateReason_Finished_ActionCount;
        }

        public void ConfigurationChanged()
        {
            CurrState = CollectionRuleState.Finished;
            CurrStateReason = Strings.Message_CollectionRuleStateReason_Finished_ConfigurationChanged;
        }

        // Untested -> Not sure when/how this is used
        public void RuleFailure()
        {
            CurrState = CollectionRuleState.Finished;
            CurrStateReason = Strings.Message_CollectionRuleStateReason_Finished_Failure;
        }
    }
}
