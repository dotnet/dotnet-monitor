// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.CollectionRuleDefaultsInterfaces;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers.EventCounterShortcuts
{
    /// <summary>
    /// Options for the CPUUsage trigger.
    /// </summary>
    internal sealed partial class CPUUsageOptions : IEventCounterShortcuts, ISlidingWindowDurationProperties
    {
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CPUUsageOptions_GreaterThan))]
        [DefaultValue(CPUUsageOptionsDefaults.GreaterThan)]
        [Range(TriggerOptionsConstants.Percentage_MinValue, TriggerOptionsConstants.Percentage_MaxValue)]
        public double? GreaterThan { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CPUUsageOptions_LessThan))]
        [Range(TriggerOptionsConstants.Percentage_MinValue, TriggerOptionsConstants.Percentage_MaxValue)]
        public double? LessThan { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_EventCounterOptions_SlidingWindowDuration))]
        [Range(typeof(TimeSpan), TriggerOptionsConstants.SlidingWindowDuration_MinValue, TriggerOptionsConstants.SlidingWindowDuration_MaxValue)]
        [DefaultValue(EventCounterOptionsDefaults.SlidingWindowDuration)]
        public TimeSpan? SlidingWindowDuration { get; set; }
    }
}
