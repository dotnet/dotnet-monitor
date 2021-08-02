// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if UNITTEST
namespace Microsoft.Diagnostics.Monitoring.TestCommon.Options
#else
namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions
#endif
{
    // Constants for action options allowing reuse among multiple actions and for tests to verify ranges.
    internal static class ActionOptionsConstants
    {
        public const int BufferSizeMegabytes_MaxValue = 1024;
        public static readonly string BufferSizeMegabytes_MaxValue_String = BufferSizeMegabytes_MaxValue.ToString();
        public const int BufferSizeMegabytes_MinValue = 1;
        public static readonly string BufferSizeMegabytes_MinValue_String = BufferSizeMegabytes_MinValue.ToString();

        public const string Duration_MaxValue = "1.00:00:00"; // 1 day
        public const string Duration_MinValue = "00:00:01"; // 1 second

        public const int MetricsIntervalSeconds_MaxValue = 24 * 60 * 60; // 1 day
        public static readonly string MetricsIntervalSeconds_MaxValue_String = MetricsIntervalSeconds_MaxValue.ToString();
        public const int MetricsIntervalSeconds_MinValue = 1; // 1 second
        public static readonly string MetricsIntervalSeconds_MinValue_String = MetricsIntervalSeconds_MinValue.ToString();
    }
}
