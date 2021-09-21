﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers
{
    /// <summary>
    /// Options for the AspNetResponseStatus trigger.
    /// </summary>
    internal sealed class AspNetResponseStatusOptions :
        IAspNetActionPathFilters
    {
        private const string StatusCodeRegex = "[1-5][0-9]{2}";
        private const string StatusCodesRegex = StatusCodeRegex + "(-" + StatusCodeRegex + ")?";

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AspNetResponseStatusOptions_StatusCodes))]
        [Required]
        [MinLength(1)]
        [RegularExpressions(StatusCodesRegex)]
        public string[] StatusCodes { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AspNetResponseStatusOptions_ResponseCount))]
        [Required]
        public int ResponseCount { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AspNetResponseStatusOptions_SlidingWindowDuration))]
        [Range(typeof(TimeSpan), TriggerOptionsConstants.SlidingWindowDuration_MinValue, TriggerOptionsConstants.SlidingWindowDuration_MaxValue)]
        [DefaultValue(AspNetResponseStatusOptionsDefaults.SlidingWindowDuration)]
        public TimeSpan? SlidingWindowDuration { get; set; }

        // CONSIDER: Currently described that paths have to exactly match one item in the list.
        // Consider allowing for wildcard/globbing to simplfy list of matchable paths.
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AspNetResponseStatusOptions_IncludePaths))]
        public string[] IncludePaths { get; set; }

        // CONSIDER: Currently described that paths have to exactly match one item in the list.
        // Consider allowing for wildcard/globbing to simplfy list of matchable paths.
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AspNetResponseStatusOptions_ExcludePaths))]
        public string[] ExcludePaths { get; set; }
    }
}
