// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    public class CollectionRulesStateReasons
    {
        public const string Running = "This collection rule is active and waiting for its triggering conditions to be satisfied.";
        public const string ExecutingActions = "This collection rule has had its triggering conditions satisfied and is currently executing its action list.";
        public const string Throttled = "This collection rule is temporarily throttled because the ActionCountLimit has been reached within the ActionCountSlidingWindowDuration.";
        public const string Finished_ConfigurationChanged = "This collection rule will no longer trigger because it no longer exists.";
        public const string Finished_RuleDuration = "The collection rule will no longer trigger because the RuleDuration limit was reached.";
        public const string Finished_Startup = "The collection rule will no longer trigger because the Startup trigger only executes once.";
        public const string Finished_Failure = "The collection rule will no longer trigger because a failure occurred.";
    }
}
