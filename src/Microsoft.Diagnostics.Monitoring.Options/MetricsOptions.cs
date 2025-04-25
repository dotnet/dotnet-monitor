// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    /// <summary>
    /// Configuration for prometheus metric collection and retrieval.
    /// TODO We may want to expose https endpoints here as well, and make port changes
    /// TODO How do we determine which process to scrape in multi-proc situations? How do we configure this
    /// for situations where the pid is not known or ambiguous?
    /// </summary>
    public class MetricsOptions
    {
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_MetricsOptions_Enabled))]
        [DefaultValue(MetricsOptionsDefaults.Enabled)]
        public bool? Enabled { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_MetricsOptions_Endpoints))]
        public string? Endpoints { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_MetricsOptions_MetricCount))]
        [DefaultValue(MetricsOptionsDefaults.MetricCount)]
        [Range(1, int.MaxValue)]
        public int? MetricCount { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_MetricsOptions_IncludeDefaultProviders))]
        [DefaultValue(MetricsOptionsDefaults.IncludeDefaultProviders)]
        public bool? IncludeDefaultProviders { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_MetricsOptions_Providers))]
        public List<MetricProvider> Providers { get; set; } = [];

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_MetricsOptions_Meters))]
        public List<MeterConfiguration> Meters { get; set; } = [];
    }

    public class MetricProvider
    {
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_MetricProvider_ProviderName))]
        [Required]
        public string ProviderName { get; set; } = string.Empty;

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_MetricProvider_CounterNames))]
        public List<string> CounterNames { get; set; } = [];
    }

    public class MeterConfiguration
    {
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_MeterConfiguration_MeterName))]
        public string? MeterName { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_MeterConfiguration_InstrumentNames))]
        public List<string> InstrumentNames { get; set; } = [];
    }
}
