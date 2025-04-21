// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.CollectionRuleDefaultsInterfaces;
using Microsoft.Extensions.Options;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers
{
    /// <summary>
    /// Options for the EventMeter trigger.
    /// </summary>
    [OptionsValidator]
    internal sealed partial class EventMeterOptions : IValidateOptions<EventMeterOptions>, ISlidingWindowDurationProperties
    {
        [Display(
            Name = nameof(MeterName),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_EventMeterOptions_MeterName))]
        [Required]
        public string MeterName { get; set; } = string.Empty;

        [Display(
            Name = nameof(InstrumentName),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_EventMeterOptions_InstrumentName))]
        [Required]
        public string InstrumentName { get; set; } = string.Empty;

        [Display(
            Name = nameof(GreaterThan),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_EventMeterOptions_GreaterThan))]
        public double? GreaterThan { get; set; }

        [Display(
            Name = nameof(LessThan),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_EventMeterOptions_LessThan))]
        public double? LessThan { get; set; }

        [Display(
            Name = nameof(SlidingWindowDuration),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_EventMeterOptions_SlidingWindowDuration))]
        [Range(typeof(TimeSpan), TriggerOptionsConstants.SlidingWindowDuration_MinValue, TriggerOptionsConstants.SlidingWindowDuration_MaxValue)]
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Addressed by DynamicDependency on ValidationHelper.TryValidateOptions method")]
        public TimeSpan? SlidingWindowDuration { get; set; }

        [Display(
            Name = nameof(HistogramPercentile),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_EventMeterOptions_HistogramPercentile))]
        [Range(TriggerOptionsConstants.Percentage_MinValue, TriggerOptionsConstants.Percentage_MaxValue)]
        public int? HistogramPercentile { get; set; }
    }
}
