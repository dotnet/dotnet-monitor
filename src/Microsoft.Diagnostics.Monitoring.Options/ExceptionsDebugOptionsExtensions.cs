// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Monitoring.Options
{
    internal static class ExceptionsDebugOptionsExtensions
    {
        public static bool GetIncludeMonitorExceptions(this ExceptionsDebugOptions? options)
        {
            return (options?.IncludeMonitorExceptions).GetValueOrDefault(ExceptionsDebugOptionsDefaults.IncludeMonitorExceptions);
        }
    }
}
