// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    // Will need an actual name for this...mostly just experimenting for now.
    internal class CollectionRulesStateUpdater
    {
        public CollectionRulesStateInternal CurrState { get; private set; }

        internal void BeginActionExecution()
        {
            CurrState = CollectionRulesStateInternal.ActionStarted;
        }

        internal void ActionExecutionSucceeded()
        {
            CurrState = CollectionRulesStateInternal.Running;
        }

        // Not sure what the use-case is here (if there is one)
        internal void ActionExecutionFailed()
        {
            CurrState = CollectionRulesStateInternal.ActionFailed;
        }

        internal void BeginThrottled()
        {
            CurrState = CollectionRulesStateInternal.Throttled;
        }

        internal void EndThrottled()
        {
            CurrState = CollectionRulesStateInternal.Running;
        }

        internal void StartupTriggerCompleted()
        {
            CurrState = CollectionRulesStateInternal.FinishedViaStartup;
        }

        internal void RuleDurationReached()
        {
            CurrState = CollectionRulesStateInternal.FinishedViaRuleDuration;
        }

        internal void ActionCountReached()
        {
            CurrState = CollectionRulesStateInternal.FinishedViaActionCount;
        }

        internal void ConfigurationChanged()
        {
            CurrState = CollectionRulesStateInternal.FinishedViaConfigChange;
        }

        // Not sure what the use-case is here (if there is one)
        internal void RuleFailure()
        {
            CurrState = CollectionRulesStateInternal.FinishedViaFailure;
        }
    }
}
