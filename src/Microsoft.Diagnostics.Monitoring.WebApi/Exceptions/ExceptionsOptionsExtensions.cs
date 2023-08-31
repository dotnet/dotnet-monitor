// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Monitoring.Options
{
    internal static class ExceptionsOptionsExtensions
    {
        public static bool GetEnabled(this ExceptionsOptions options)
        {
            return options.Enabled.GetValueOrDefault(ExceptionsOptionsDefaults.Enabled);
        }

        public static int GetTopLevelLimit(this ExceptionsOptions options)
        {
            return options.TopLevelLimit.GetValueOrDefault(ExceptionsOptionsDefaults.TopLevelLimit);
        }
    }
}
