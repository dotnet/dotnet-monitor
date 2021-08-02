// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System;
using System.ComponentModel.DataAnnotations;

#if UNITTEST
namespace Microsoft.Diagnostics.Monitoring.TestCommon.Options
#else
namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options
#endif
{
    /// <summary>
    /// Options for limiting the execution of a collection rule.
    /// </summary>
    internal sealed class CollectionRuleLimitsOptions
    {
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectionRuleLimitsOptions_ActionCount))]
        public int? ActionCount { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectionRuleLimitsOptions_ActionCountSlidingWindowDuration))]
        [Range(typeof(TimeSpan), "00:00:01", "1.00:00:00")]
        public TimeSpan? ActionCountSlidingWindowDuration { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectionRuleLimitsOptions_RuleDuration))]
        [Range(typeof(TimeSpan), "00:00:01", "365.00:00:00")]
        public TimeSpan? RuleDuration { get; set; }
    }
}
