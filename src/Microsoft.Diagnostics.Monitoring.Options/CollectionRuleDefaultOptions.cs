// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal class CollectionRuleDefaultOptions
    {
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectionRuleDefaultOptions_EgressProvider))]
        public string EgressProvider { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectionRuleDefaultOptions_SlidingWindowDuration))]
        //[Range(typeof(TimeSpan), TriggerOptionsConstants.SlidingWindowDuration_MinValue, TriggerOptionsConstants.SlidingWindowDuration_MaxValue)]
        public TimeSpan? SlidingWindowDuration { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectionRuleDefaultOptions_ActionCount))]
        //[DefaultValue(CollectionRuleLimitsOptionsDefaults.ActionCount)]
        public int? ActionCount { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectionRuleDefaultOptions_ActionCountSlidingWindowDuration))]
        //[Range(typeof(TimeSpan), CollectionRuleOptionsConstants.ActionCountSlidingWindowDuration_MinValue, CollectionRuleOptionsConstants.ActionCountSlidingWindowDuration_MaxValue)]
        public TimeSpan? ActionCountSlidingWindowDuration { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectionRuleDefaultOptions_RuleDuration))]
        //[Range(typeof(TimeSpan), CollectionRuleOptionsConstants.RuleDuration_MinValue, CollectionRuleOptionsConstants.RuleDuration_MaxValue)]
        public TimeSpan? RuleDuration { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectionRuleDefaultOptions_RequestCount))]
        public int? RequestCount { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AspNetResponseStatusOptions_ResponseCount))]
        public int? ResponseCount { get; set; }
    }
}