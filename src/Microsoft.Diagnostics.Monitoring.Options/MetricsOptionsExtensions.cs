﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal static class MetricsOptionsExtensions
    {
        public static bool GetEnabled(this MetricsOptions options)
        {
            return options.Enabled.GetValueOrDefault(MetricsOptionsDefaults.Enabled);
        }
    }
}
