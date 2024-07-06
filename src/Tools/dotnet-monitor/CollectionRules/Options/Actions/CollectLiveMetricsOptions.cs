// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.CollectionRuleDefaultsInterfaces;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions
{
    /// <summary>
    /// Options for the CollectLiveMetrics action.
    /// </summary>
    [DebuggerDisplay("CollectLiveMetrics")]
#if SCHEMAGEN
    [NJsonSchema.Annotations.JsonSchemaFlatten]
#endif
    internal sealed partial record class CollectLiveMetricsOptions : BaseRecordOptions, IEgressProviderProperties
    {
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectLiveMetricsOptions_IncludeDefaultProviders))]
        [DefaultValue(CollectLiveMetricsOptionsDefaults.IncludeDefaultProviders)]
        public bool? IncludeDefaultProviders { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectLiveMetricsOptions_Providers))]
        public EventMetricsProvider[]? Providers { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectLiveMetricsOptions_Meters))]
        public EventMetricsMeter[]? Meters { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectArtifactOptions_Duration))]
        [Range(typeof(TimeSpan), ActionOptionsConstants.Duration_MinValue, ActionOptionsConstants.Duration_MaxValue)]
        [DefaultValue(CollectLiveMetricsOptionsDefaults.Duration)]
        public TimeSpan? Duration { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectArtifactOptions_Egress))]
        [Required(
            ErrorMessageResourceType = typeof(OptionsDisplayStrings),
            ErrorMessageResourceName = nameof(OptionsDisplayStrings.ErrorMessage_NoDefaultEgressProvider))]
#if !UNITTEST && !SCHEMAGEN
        [ValidateEgressProvider]
#endif
        public string Egress { get; set; } = string.Empty;

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectArtifactOptions_ArtifactName))]
        public string? ArtifactName { get; set; }
    }
}
