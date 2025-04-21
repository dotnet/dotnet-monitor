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
    /// Options for the AspNetResponseStatus trigger.
    /// </summary>
    [OptionsValidator]
    internal sealed partial class AspNetResponseStatusOptions :
        IAspNetActionPathFilters, ISlidingWindowDurationProperties,
        IValidateOptions<AspNetResponseStatusOptions>
    {
        private const string StatusCodeRegex = "[1-5][0-9]{2}";
        private const string StatusCodesRegex = StatusCodeRegex + "(-" + StatusCodeRegex + ")?";

#nullable disable
        [Display(
            Name = nameof(StatusCodes),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AspNetResponseStatusOptions_StatusCodes))]
        [Required]
        [MinLength(1)]
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Count of string property is preserved by DynamicDependency.")]
        [RegularExpressions(StatusCodesRegex,
            ErrorMessageResourceType = typeof(OptionsDisplayStrings),
            ErrorMessageResourceName = nameof(OptionsDisplayStrings.ErrorMessage_StatusCodesRegularExpressionDoesNotMatch))]
        public string[] StatusCodes { get; set; }
#nullable enable

        [Display(
            Name = nameof(ResponseCount),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AspNetResponseStatusOptions_ResponseCount))]

        [Required(
            ErrorMessageResourceType = typeof(OptionsDisplayStrings),
            ErrorMessageResourceName = nameof(OptionsDisplayStrings.ErrorMessage_NoDefaultResponseCount))]
        [Range(1, int.MaxValue)]
        public int ResponseCount { get; set; }

        [Display(
            Name = nameof(SlidingWindowDuration),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AspNetResponseStatusOptions_SlidingWindowDuration))]
        [Range(typeof(TimeSpan), TriggerOptionsConstants.SlidingWindowDuration_MinValue, TriggerOptionsConstants.SlidingWindowDuration_MaxValue)]
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Addressed by DynamicDependency on ValidationHelper.TryValidateOptions method")]
        public TimeSpan? SlidingWindowDuration { get; set; }

        // CONSIDER: Currently described that paths have to exactly match one item in the list.
        // Consider allowing for wildcard/globbing to simplify list of matchable paths.
        [Display(
            Name = nameof(IncludePaths),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AspNetResponseStatusOptions_IncludePaths))]
        public string[]? IncludePaths { get; set; }

        // CONSIDER: Currently described that paths have to exactly match one item in the list.
        // Consider allowing for wildcard/globbing to simplify list of matchable paths.
        [Display(
            Name = nameof(ExcludePaths),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AspNetResponseStatusOptions_ExcludePaths))]
        public string[]? ExcludePaths { get; set; }
    }
}
