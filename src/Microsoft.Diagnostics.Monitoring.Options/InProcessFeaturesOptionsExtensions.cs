﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Diagnostics.Monitoring.Options
{
    internal static class InProcessFeaturesOptionsExtensions
    {
        public static bool GetEnabled(this InProcessFeaturesOptions options)
        {
            return options.Enabled.GetValueOrDefault(InProcessFeaturesOptionsDefaults.Enabled);
        }
    }
}
