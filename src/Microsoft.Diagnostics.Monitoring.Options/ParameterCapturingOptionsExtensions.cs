// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Monitoring.Options
{
    internal static class ParameterCapturingOptionsExtensions
    {
        public static bool GetEnabled(this ParameterCapturingOptions options)
        {
            return options.Enabled.GetValueOrDefault(ParameterCapturingOptionsDefaults.Enabled);
        }
    }
}
