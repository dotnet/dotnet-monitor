// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_MetricsOptions_MaxHistograms))]
        [DefaultValue(GlobalCounterOptionsDefaults.MaxHistograms)]
        [Range(1, int.MaxValue)]
        public int? MaxHistograms { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_MetricsOptions_MaxTimeSeries))]
        [DefaultValue(GlobalCounterOptionsDefaults.MaxTimeSeries)]
        [Range(1, int.MaxValue)]
        public int? MaxTimeSeries { get; set; }
    }

    internal static class GlobalCounterOptionsExtensions
    {
        public static float GetIntervalSeconds(this GlobalCounterOptions options) =>
            options.IntervalSeconds.GetValueOrDefault(GlobalCounterOptionsDefaults.IntervalSeconds);

        public static int GetMaxHistograms(this GlobalCounterOptions options) =>
            options.MaxHistograms.GetValueOrDefault(GlobalCounterOptionsDefaults.MaxHistograms);

        public static int GetMaxTimeSeries(this GlobalCounterOptions options) =>
            options.MaxTimeSeries.GetValueOrDefault(GlobalCounterOptionsDefaults.MaxTimeSeries);
    }
}
