// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Monitoring.Options
{
    internal static class CallStacksOptionsExtensions
    {
        public static bool GetEnabled(this CallStacksOptions options)
        {
            return options.Enabled.GetValueOrDefault(CallStacksOptionsDefaults.Enabled);
        }
    }
}
