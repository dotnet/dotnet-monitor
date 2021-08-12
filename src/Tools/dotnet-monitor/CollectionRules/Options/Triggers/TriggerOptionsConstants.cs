﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers
{
    // Constants for trigger options allowing reuse among multiple trigger and for tests to verify ranges.
    internal static class TriggerOptionsConstants
    {
        public const int CounterFrequency_MaxValue = 24*60*60; // 1 day
        public static readonly string Frequency_MaxValue_String = CounterFrequency_MaxValue.ToString(CultureInfo.InvariantCulture);
        public const int CounterFrequency_MinValue = 1; // 1 second
        public static readonly string Frequency_MinValue_String = CounterFrequency_MinValue.ToString(CultureInfo.InvariantCulture);

        public const string SlidingWindowDuration_Default = "00:01:00"; // 1 minute
        public const string SlidingWindowDuration_MaxValue = "1.00:00:00"; // 1 day
        public const string SlidingWindowDuration_MinValue = "00:00:01"; // 1 second
    }
}
