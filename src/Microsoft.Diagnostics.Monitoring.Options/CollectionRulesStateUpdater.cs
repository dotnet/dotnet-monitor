// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    // Will need an actual name for this...mostly just experimenting for now.
    internal class CollectionRulesStateUpdater
    {
        internal CollectionRulesStateInternal CurrState { get; private set; }

        internal void BeginActionExecution()
        {
            CurrState = CollectionRulesStateInternal.ActionStarted;
        }

        internal void ActionExecutionSucceeded()
        {
            CurrState = CollectionRulesStateInternal.ActionSucceeded;
        }

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

        }

        internal void StartupTriggerCompleted()
        {
            CurrState = CollectionRulesStateInternal.FinishedViaStartup;
        }

        internal void RuleDurationReached()
        {
            CurrState = CollectionRulesStateInternal.FinishedViaRuleDuration;
        }
    }
}
