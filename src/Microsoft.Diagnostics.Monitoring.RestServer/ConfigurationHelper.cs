// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Diagnostics.Monitoring.RestServer
{
    internal static class ConfigurationHelper
    {
        private const char ValueSeparator = ';';
        private static readonly char[] ValueSeparatorArray = new char[] { ValueSeparator };
        private static readonly string ValueSeparatorString = ValueSeparator.ToString();

        public static string[] SplitValue(string value)
        {
            return value.Split(ValueSeparatorArray, StringSplitOptions.RemoveEmptyEntries);
        }

        public static string JoinValue(string[] values)
        {
            return string.Join(ValueSeparatorString, values);
        }
    }
}
