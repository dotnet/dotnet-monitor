// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.CollectionRuleDefaultsInterfaces;
using System;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers
{
    /// <summary>
    /// Options for the AspNetRequestCount trigger.
    /// </summary>
    internal sealed class AspNetRequestCountOptions :
        IAspNetActionPathFilters, ISlidingWindowDurationProperties, IRequestCountProperties
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
