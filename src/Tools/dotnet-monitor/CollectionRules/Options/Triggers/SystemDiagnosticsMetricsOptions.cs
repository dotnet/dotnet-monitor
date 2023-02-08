// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.EventPipe.Triggers.SystemDiagnosticsMetrics;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.CollectionRuleDefaultsInterfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers
{
    /// <summary>
    /// Options for the SystemDiagnosticsMetrics trigger.
    /// </summary>
    internal sealed partial class SystemDiagnosticsMetricsOptions : ISlidingWindowDurationProperties
    {
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_SystemDiagnosticsMetricsOptions_ProviderName))]
        [Required]
        public string ProviderName { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_SystemDiagnosticsMetricsOptions_InstrumentName))]
        [Required]
        public string InstrumentName { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_SystemDiagnosticsMetricsOptions_GreaterThan))]
        public double? GreaterThan { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_SystemDiagnosticsMetricsOptions_LessThan))]
        public double? LessThan { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_SystemDiagnosticsMetricsOptions_SlidingWindowDuration))]
        [Range(typeof(TimeSpan), TriggerOptionsConstants.SlidingWindowDuration_MinValue, TriggerOptionsConstants.SlidingWindowDuration_MaxValue)]
        public TimeSpan? SlidingWindowDuration { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_SystemDiagnosticsMetricsOptions_HistogramPercentiles))]
        public IDictionary<string, double> HistogramPercentiles { get; set; }
            = new Dictionary<string, double>(0);

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_SystemDiagnosticsMetricsOptions_HistogramPercentiles))]
        public HistogramMode? HistogramMode { get; set; }
    }
}
