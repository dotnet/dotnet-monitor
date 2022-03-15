// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers
{
    /// <summary>
    /// Options for the ExceptionCount trigger.
    /// </summary>
    internal sealed partial class ExceptionCountOptions : IEventCounterShortcuts
    {
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_EventCounterOptions_GreaterThan))]
        [DefaultValue(ExceptionCountOptionsDefaults.GreaterThan)]
        [Range(0, double.MaxValue)]
        public double? GreaterThan { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_EventCounterOptions_LessThan))]

        [Range(0, double.MaxValue)]
        public double? LessThan { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_EventCounterOptions_SlidingWindowDuration))]
        [Range(typeof(TimeSpan), TriggerOptionsConstants.SlidingWindowDuration_MinValue, TriggerOptionsConstants.SlidingWindowDuration_MaxValue)]
        [DefaultValue(EventCounterOptionsDefaults.SlidingWindowDuration)]
        public TimeSpan? SlidingWindowDuration { get; set; } // Need to integrate with defaults here
    }
}