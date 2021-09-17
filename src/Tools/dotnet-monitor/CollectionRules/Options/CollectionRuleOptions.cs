﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options
{
    /// <summary>
    /// Options for describing an entire collection rule.
    /// </summary>
    internal sealed partial class CollectionRuleOptions
    {
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectionRuleOptions_Filters))]
        public List<ProcessFilterDescriptor> Filters { get; } = new List<ProcessFilterDescriptor>(0);

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectionRuleOptions_Trigger))]
        [Required]
        public CollectionRuleTriggerOptions Trigger { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectionRuleOptions_Actions))]
        public List<CollectionRuleActionOptions> Actions { get; } = new List<CollectionRuleActionOptions>(0);

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectionRuleOptions_Limits))]
        public CollectionRuleLimitsOptions Limits { get; set; }
    }
}
