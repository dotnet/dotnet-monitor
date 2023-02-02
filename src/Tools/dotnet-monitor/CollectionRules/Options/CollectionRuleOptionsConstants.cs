// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options
{
    internal static class CollectionRuleOptionsConstants
    {
        public const string ActionCountSlidingWindowDuration_MaxValue = "1.00:00:00"; // 1 day
        public const string ActionCountSlidingWindowDuration_MinValue = "00:00:01"; // 1 second

        public const string RuleDuration_MaxValue = "365.00:00:00"; // 1 year
        public const string RuleDuration_MinValue = "00:00:01"; // 1 second
    }
}
