// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules
{
    // NEED ANOTHER ONE FOR JUST DO THE ACTIONCOUNT AND THERE ISN'T AN ACTIONCOUNTSLIDINGWINDOWDURATION
    internal enum CollectionRuleCleanupExplanation
    {
        RuleDurationExceeded,
        ConfigurationChanged
    }
}
