// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    public class GlobalCounterOptions
    {
        public const float IntervalMinSeconds = 1;
        public const float IntervalMaxSeconds = 60 * 60 * 24; // One day

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_GlobalCounterOptions_IntervalSeconds))]
        [Range(IntervalMinSeconds, IntervalMaxSeconds)]
        [DefaultValue(GlobalCounterOptionsDefaults.IntervalSeconds)]
        public float? IntervalSeconds { get; set; }
    }

    internal static class GlobalCounterOptionsExtensions
    {
        public static float GetIntervalSeconds(this GlobalCounterOptions options) =>
            options.IntervalSeconds.GetValueOrDefault(GlobalCounterOptionsDefaults.IntervalSeconds);
    }
}
