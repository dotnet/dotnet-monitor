// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    // Will need an actual name for this...mostly just experimenting for now.
    internal class CollectionRulesStateHolder
    {
        private CollectionRulesMicroState CurrMicroState { get; set; }

        public Tuple<CollectionRulesState, string> GetCollectionRulesState()
        {
            switch (CurrMicroState)
            {
                case CollectionRulesMicroState.Running:
                    return new(CollectionRulesState.Running, CollectionRulesStateReasons.Running);
                case CollectionRulesMicroState.ActionStarted:
                    return new(CollectionRulesState.ActionExecuting, CollectionRulesStateReasons.ExecutingActions);
                case CollectionRulesMicroState.ActionFailed:
                    // What does it mean if we're in this scenario?
                    break;
                case CollectionRulesMicroState.FinishedViaFailure:
                    return new(CollectionRulesState.Finished, CollectionRulesStateReasons.Finished_Failure);
                case CollectionRulesMicroState.FinishedViaRuleDuration:
                    return new(CollectionRulesState.Finished, CollectionRulesStateReasons.Finished_RuleDuration);
                case CollectionRulesMicroState.FinishedViaConfigChange:
                    return new(CollectionRulesState.Finished, CollectionRulesStateReasons.Finished_ConfigurationChanged);
                case CollectionRulesMicroState.FinishedViaStartup:
                    return new(CollectionRulesState.Finished, CollectionRulesStateReasons.Finished_Startup);
                case CollectionRulesMicroState.FinishedViaActionCount:
                    return new(CollectionRulesState.Finished, CollectionRulesStateReasons.Finished_ActionCount);
                case CollectionRulesMicroState.Throttled:
                    return new(CollectionRulesState.Throttled, CollectionRulesStateReasons.Throttled);
            }

            // Need to handle default case better
            return new(CollectionRulesState.Running, CollectionRulesStateReasons.Running);
        }

        internal void BeginActionExecution()
        {
            CurrMicroState = CollectionRulesMicroState.ActionStarted;
        }

        internal void ActionExecutionSucceeded()
        {
            CurrMicroState = CollectionRulesMicroState.Running;
        }

        internal void ActionExecutionFailed()
        {
            CurrMicroState = CollectionRulesMicroState.ActionFailed;
        }

        internal void BeginThrottled()
        {
            CurrMicroState = CollectionRulesMicroState.Throttled;
        }

        internal void EndThrottled()
        {
            if (CurrMicroState == CollectionRulesMicroState.Throttled)
            {
                CurrMicroState = CollectionRulesMicroState.Running;
            }
        }

        internal void StartupTriggerCompleted()
        {
            CurrMicroState = CollectionRulesMicroState.FinishedViaStartup;
        }

        internal void RuleDurationReached()
        {
            CurrMicroState = CollectionRulesMicroState.FinishedViaRuleDuration;
        }

        internal void ActionCountReached()
        {
            CurrMicroState = CollectionRulesMicroState.FinishedViaActionCount;
        }

        internal void ConfigurationChanged()
        {
            CurrMicroState = CollectionRulesMicroState.FinishedViaConfigChange;
        }

        // Not sure what the use-case is here (if there is one)
        internal void RuleFailure()
        {
            CurrMicroState = CollectionRulesMicroState.FinishedViaFailure;
        }
    }
}
