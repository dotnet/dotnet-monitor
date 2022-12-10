// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    internal static class TriggerTestsConstants
    {
        public static readonly string[] ExpectedStatusCodes = { "400", "500" };

        public const int ExpectedRequestCount = 1;
        public const int UnknownRequestCount = 10;

        public const int ExpectedResponseCount = 2;
        public const int UnknownResponseCount = 20;

        public const string ExpectedSlidingWindowDuration = "00:00:03";
        public const string UnknownSlidingWindowDuration = "00:00:30";
    }
}
