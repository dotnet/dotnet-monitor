// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    public class GlobalCounterOptions
    {
        [Range(1, 3600 * 24)]
        public int? IntervalSeconds { get; set; }
    }

    internal static class GlobalCounterOptionsExtensions
    {
        public static int GetIntervalSeconds(this GlobalCounterOptions options) =>
            options.IntervalSeconds.GetValueOrDefault(GlobalCounterOptionsDefaults.IntervalSeconds);
    }
}
