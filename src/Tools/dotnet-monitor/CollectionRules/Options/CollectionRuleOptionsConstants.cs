// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if UNITTEST
namespace Microsoft.Diagnostics.Monitoring.TestCommon.Options
#else
namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options
#endif
{
    internal static class CollectionRuleOptionsConstants
    {
        public const string ActionCountSlidingWindowDuration_MaxValue = "1.00:00:00"; // 1 day
        public const string ActionCountSlidingWindowDuration_MinValue = "00:00:01"; // 1 second

        public const string RuleDuration_MaxValue = "365.00:00:00"; // 1 day
        public const string RuleDuration_MinValue = "00:00:01"; // 1 second
    }
}
