// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options
{
    /// <summary>
    /// Options for limiting the execution of a collection rule.
    /// </summary>
    internal sealed class CollectionRuleLimitsOptions
    {
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectionRuleLimitsOptions_ActionCount))]
        [DefaultValue(CollectionRuleLimitsOptionsDefaults.ActionCount)]
        [Range(1, int.MaxValue)]
        public int? ActionCount { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectionRuleLimitsOptions_ActionCountSlidingWindowDuration))]
        [Range(typeof(TimeSpan), CollectionRuleOptionsConstants.ActionCountSlidingWindowDuration_MinValue, CollectionRuleOptionsConstants.ActionCountSlidingWindowDuration_MaxValue)]
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Addressed by DynamicDependency on ValidationHelper.TryValidateOptions method")]
        public TimeSpan? ActionCountSlidingWindowDuration { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectionRuleLimitsOptions_RuleDuration))]
        [Range(typeof(TimeSpan), CollectionRuleOptionsConstants.RuleDuration_MinValue, CollectionRuleOptionsConstants.RuleDuration_MaxValue)]
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Addressed by DynamicDependency on ValidationHelper.TryValidateOptions method")]
        public TimeSpan? RuleDuration { get; set; }
    }
}
