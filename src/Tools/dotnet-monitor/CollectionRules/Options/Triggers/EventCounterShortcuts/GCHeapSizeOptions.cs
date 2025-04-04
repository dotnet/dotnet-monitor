// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.CollectionRuleDefaultsInterfaces;
using Microsoft.Extensions.Options;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers.EventCounterShortcuts
{
    /// <summary>
    /// Options for the GCHeapSize trigger.
    /// </summary>
    [OptionsValidator]
    internal sealed partial class GCHeapSizeOptions : IValidateOptions<GCHeapSizeOptions>, IEventCounterShortcuts, ISlidingWindowDurationProperties
    {
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_GCHeapSizeOptions_GreaterThan))]
        [DefaultValue(GCHeapSizeOptionsDefaults.GreaterThan)]
        [Range(0, double.MaxValue)]
        public double? GreaterThan { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_GCHeapSizeOptions_LessThan))]
        [Range(0, double.MaxValue)]
        public double? LessThan { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_EventCounterOptions_SlidingWindowDuration))]
        [Range(typeof(TimeSpan), TriggerOptionsConstants.SlidingWindowDuration_MinValue, TriggerOptionsConstants.SlidingWindowDuration_MaxValue)]
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Addressed by DynamicDependency on ValidationHelper.TryValidateOptions method")]
        [DefaultValue(EventCounterOptionsDefaults.SlidingWindowDuration)]
        public TimeSpan? SlidingWindowDuration { get; set; }
    }
}
