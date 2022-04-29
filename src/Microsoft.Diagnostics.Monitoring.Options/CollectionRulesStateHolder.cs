// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal class CollectionRulesStateHolder
    {
        public CollectionRulesState CurrState { get; private set; }
        public string CurrStateReason { get; private set; }

        internal void BeginActionExecution()
        {
            CurrState = CollectionRulesState.ActionExecuting;
            CurrStateReason = CollectionRulesStateReasons.ExecutingActions;
        }

        internal void ActionExecutionSucceeded()
        {
            CurrState = CollectionRulesState.Running;
            CurrStateReason = CollectionRulesStateReasons.Running;
        }


        // NOT SURE WHAT TO DO HERE
        internal void ActionExecutionFailed()
        {

        }

        internal void BeginThrottled()
        {
            CurrState = CollectionRulesState.Throttled;
            CurrStateReason = CollectionRulesStateReasons.Throttled;
        }

        internal void EndThrottled()
        {
            if (CurrState == CollectionRulesState.Throttled)
            {
                CurrState = CollectionRulesState.Running;
                CurrStateReason = CollectionRulesStateReasons.Running;
            }
        }

        internal void StartupTriggerCompleted()
        {
            CurrState = CollectionRulesState.Finished;
            CurrStateReason = CollectionRulesStateReasons.Finished_Startup;
        }

        internal void RuleDurationReached()
        {
            CurrState = CollectionRulesState.Finished;
            CurrStateReason = CollectionRulesStateReasons.Finished_RuleDuration;
        }

        internal void ActionCountReached()
        {
            CurrState = CollectionRulesState.Finished;
            CurrStateReason = CollectionRulesStateReasons.Finished_ActionCount;
        }

        internal void ConfigurationChanged()
        {
            CurrState = CollectionRulesState.Finished;
            CurrStateReason = CollectionRulesStateReasons.Finished_ConfigurationChanged;
        }

        // Not sure what the use-case is here (if there is one)
        internal void RuleFailure()
        {
            CurrState = CollectionRulesState.Finished;
            CurrStateReason = CollectionRulesStateReasons.Finished_Failure;
        }
    }
}
