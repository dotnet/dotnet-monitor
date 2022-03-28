// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options
{
    internal class CollectionRuleDefaultsOptions
    {
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectionRuleDefaultsOptions_Triggers))]
        public CollectionRuleTriggerDefaultsOptions Triggers { get; set; } = new();

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectionRuleDefaultsOptions_Actions))]
        public CollectionRuleActionDefaultsOptions Actions { get; set; } = new();

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectionRuleDefaultsOptions_Limits))]
        public CollectionRuleLimitsDefaultsOptions Limits { get; set; } = new();
    }
}
