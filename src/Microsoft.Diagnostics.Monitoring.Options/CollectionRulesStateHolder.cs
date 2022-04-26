// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    // Will need an actual name for this...mostly just experimenting for now.
    internal class CollectionRulesStateHolder
    {
        public CollectionRulesMicroState CurrState { get; private set; }

        internal void BeginActionExecution()
        {
            CurrState = CollectionRulesMicroState.ActionStarted;
        }

        internal void ActionExecutionSucceeded()
        {
            CurrState = CollectionRulesMicroState.Running;
        }

        internal void ActionExecutionFailed()
        {
            CurrState = CollectionRulesMicroState.ActionFailed;
        }

        internal void BeginThrottled()
        {
            CurrState = CollectionRulesMicroState.Throttled;
        }

        internal void EndThrottled()
        {
            if (CurrState == CollectionRulesMicroState.Throttled)
            {
                CurrState = CollectionRulesMicroState.Running;
            }
        }

        internal void StartupTriggerCompleted()
        {
            CurrState = CollectionRulesMicroState.FinishedViaStartup;
        }

        internal void RuleDurationReached()
        {
            CurrState = CollectionRulesMicroState.FinishedViaRuleDuration;
        }

        internal void ActionCountReached()
        {
            CurrState = CollectionRulesMicroState.FinishedViaActionCount;
        }

        internal void ConfigurationChanged()
        {
            CurrState = CollectionRulesMicroState.FinishedViaConfigChange;
        }

        // Not sure what the use-case is here (if there is one)
        internal void RuleFailure()
        {
            CurrState = CollectionRulesMicroState.FinishedViaFailure;
        }
    }
}
