// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers
{
    // Constants for trigger options allowing reuse among multiple trigger and for tests to verify ranges.
    internal static class TriggerOptionsConstants
    {
        public const string SlidingWindowDuration_Default = "00:01:00"; // 1 minute
        public const string SlidingWindowDuration_MaxValue = "1.00:00:00"; // 1 day
        public const string SlidingWindowDuration_MinValue = "00:00:01"; // 1 second

        public const double Percentage_MinValue = 0;
        public const double Percentage_MaxValue = 100;
    }
}
