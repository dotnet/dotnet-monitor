// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.Diagnostics.Monitoring.WebApi;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options
{
    internal sealed class CollectionRuleDefaultsOptions
    {
        [Display(
            Name = nameof(Triggers),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectionRuleDefaultsOptions_Triggers))]
        public CollectionRuleTriggerDefaultsOptions? Triggers { get; set; }

        [Display(
            Name = nameof(Actions),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectionRuleDefaultsOptions_Actions))]
        public CollectionRuleActionDefaultsOptions? Actions { get; set; }

        [Display(
            Name = nameof(Limits),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectionRuleDefaultsOptions_Limits))]
        public CollectionRuleLimitsDefaultsOptions? Limits { get; set; }
    }
}
