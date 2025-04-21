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
    /// Options for the EventCounter trigger.
    /// </summary>
    [OptionsValidator]
    internal sealed partial class EventCounterOptions : IValidateOptions<EventCounterOptions>, ISlidingWindowDurationProperties
    {
        [Display(
            Name = nameof(ProviderName),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_EventCounterOptions_ProviderName))]
        [Required]
        public string ProviderName { get; set; } = string.Empty;

        [Display(
            Name = nameof(CounterName),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_EventCounterOptions_CounterName))]
        [Required]
        public string CounterName { get; set; } = string.Empty;

        [Display(
            Name = nameof(GreaterThan),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_EventCounterOptions_GreaterThan))]
        public double? GreaterThan { get; set; }

        [Display(
            Name = nameof(LessThan),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_EventCounterOptions_LessThan))]
        public double? LessThan { get; set; }

        [Display(
            Name = nameof(SlidingWindowDuration),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_EventCounterOptions_SlidingWindowDuration))]
        [Range(typeof(TimeSpan), TriggerOptionsConstants.SlidingWindowDuration_MinValue, TriggerOptionsConstants.SlidingWindowDuration_MaxValue)]
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Addressed by DynamicDependency on ValidationHelper.TryValidateOptions method")]
        public TimeSpan? SlidingWindowDuration { get; set; }
    }
}
