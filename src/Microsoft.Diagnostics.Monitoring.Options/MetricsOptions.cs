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
            Name = nameof(Enabled),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_MetricsOptions_Enabled))]
        [DefaultValue(MetricsOptionsDefaults.Enabled)]
        public bool? Enabled { get; set; }

        [Display(
            Name = nameof(Endpoints),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_MetricsOptions_Endpoints))]
        public string? Endpoints { get; set; }

        [Display(
            Name = nameof(MetricCount),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_MetricsOptions_MetricCount))]
        [DefaultValue(MetricsOptionsDefaults.MetricCount)]
        [Range(1, int.MaxValue)]
        public int? MetricCount { get; set; }

        [Display(
            Name = nameof(IncludeDefaultProviders),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_MetricsOptions_IncludeDefaultProviders))]
        [DefaultValue(MetricsOptionsDefaults.IncludeDefaultProviders)]
        public bool? IncludeDefaultProviders { get; set; }

        [Display(
            Name = nameof(Providers),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_MetricsOptions_Providers))]
        public List<MetricProvider> Providers { get; set; } = [];

        [Display(
            Name = nameof(Meters),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_MetricsOptions_Meters))]
        public List<MeterConfiguration> Meters { get; set; } = [];
    }

    public class MetricProvider
    {
        [Display(
            Name = nameof(ProviderName),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_MetricProvider_ProviderName))]
        [Required]
        public string ProviderName { get; set; } = string.Empty;

        [Display(
            Name = nameof(CounterNames),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_MetricProvider_CounterNames))]
        public List<string> CounterNames { get; set; } = [];
    }

    public class MeterConfiguration
    {
        [Display(
            Name = nameof(MeterName),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_MeterConfiguration_MeterName))]
        public string? MeterName { get; set; }

        [Display(
            Name = nameof(InstrumentNames),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_MeterConfiguration_InstrumentNames))]
        public List<string> InstrumentNames { get; set; } = [];
    }
}
