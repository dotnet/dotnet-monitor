// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    internal static class LimitsTestsConstants
    {
        public const int ExpectedActionCount = 4;
        public const int UnknownActionCount = 40;

        public const string ExpectedActionCountSlidingWindowDuration = "00:00:05";
        public const string UnknownActionCountSlidingWindowDuration = "00:00:50";

        public const string ExpectedRuleDuration = "00:00:11";
        public const string UnknownRuleDuration = "00:01:10";
    }
}
