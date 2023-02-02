// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions
{
    // Constants for action options allowing reuse among multiple actions and for tests to verify ranges.
    internal static class ActionOptionsConstants
    {
        public const int BufferSizeMegabytes_MaxValue = 1024;
        public static readonly string BufferSizeMegabytes_MaxValue_String = BufferSizeMegabytes_MaxValue.ToString(CultureInfo.InvariantCulture);
        public const int BufferSizeMegabytes_MinValue = 1;
        public static readonly string BufferSizeMegabytes_MinValue_String = BufferSizeMegabytes_MinValue.ToString(CultureInfo.InvariantCulture);

        public const string Duration_MaxValue = "1.00:00:00"; // 1 day
        public const string Duration_MinValue = "00:00:01"; // 1 second
    }
}
