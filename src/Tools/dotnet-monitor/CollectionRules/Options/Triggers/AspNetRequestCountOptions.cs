// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.CollectionRuleDefaultsInterfaces;
using Microsoft.Extensions.Options;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers
{
    /// <summary>
    /// Options for the AspNetRequestCount trigger.
    /// </summary>
    [OptionsValidator]
    internal sealed partial class AspNetRequestCountOptions :
        IAspNetActionPathFilters, ISlidingWindowDurationProperties, IRequestCountProperties,
        IValidateOptions<AspNetRequestCountOptions>
    {
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AspNetRequestCountOptions_RequestCount))]

        [Required(
            ErrorMessageResourceType = typeof(OptionsDisplayStrings),
            ErrorMessageResourceName = nameof(OptionsDisplayStrings.ErrorMessage_NoDefaultRequestCount))]
        [Range(1, int.MaxValue)]
        public int RequestCount { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AspNetRequestCountOptions_SlidingWindowDuration))]
        [Range(typeof(TimeSpan), TriggerOptionsConstants.SlidingWindowDuration_MinValue, TriggerOptionsConstants.SlidingWindowDuration_MaxValue)]
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Addressed by DynamicDependency on ValidationHelper.TryValidateOptions method")]
        public TimeSpan? SlidingWindowDuration { get; set; }

        // CONSIDER: Currently described that paths have to exactly match one item in the list.
        // Consider allowing for wildcard/globbing to simplify list of matchable paths.
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AspNetRequestCountOptions_IncludePaths))]
        public string[]? IncludePaths { get; set; }

        // CONSIDER: Currently described that paths have to exactly match one item in the list.
        // Consider allowing for wildcard/globbing to simplify list of matchable paths.
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AspNetRequestCountOptions_ExcludePaths))]
        public string[]? ExcludePaths { get; set; }
    }
}
