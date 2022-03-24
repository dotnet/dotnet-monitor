// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    // Will need to reach consensus on what these terms should be that is widely understood
    public enum CollectionRulesState
    {
        Running, // Collection Rule is waiting for triggering condition to be met
        Collecting, // Trigger has been triggered -> actions are being executed
        WaitingToResume, // ActionCount has been hit within the ActionCountSlidingWindowDuration -> waiting to resume
        Finished // Collection Rule is done executing permanently -> exceeded RuleDuration
    }
}