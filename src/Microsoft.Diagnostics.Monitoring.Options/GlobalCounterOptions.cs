// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    public class GlobalCounterOptions
    {
        public const int IntervalMinSeconds = 1;
        public const int IntervalMaxSeconds = 60 * 60 * 24; // One day

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_GlobalCounterOptions_IntervalSeconds))]
        [Range(IntervalMinSeconds, IntervalMaxSeconds)]
        [DefaultValue(GlobalCounterOptionsDefaults.IntervalSeconds)]
        public int? IntervalSeconds { get; set; }
    }

    internal static class GlobalCounterOptionsExtensions
    {
        public static int GetIntervalSeconds(this GlobalCounterOptions options) =>
            options.IntervalSeconds.GetValueOrDefault(GlobalCounterOptionsDefaults.IntervalSeconds);
    }
}
