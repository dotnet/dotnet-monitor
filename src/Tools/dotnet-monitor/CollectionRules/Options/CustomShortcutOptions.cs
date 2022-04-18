// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options
{
    /// <summary>
    /// Options for describing custom user-defined shortcuts.
    /// </summary>
    internal sealed partial class CustomShortcutOptions
    {
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectionRuleOptions_Filters))]
        public IDictionary<string, ProcessFilterDescriptor> Filters { get; } = new Dictionary<string, ProcessFilterDescriptor>();

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectionRuleOptions_Trigger))]
        [Required]
        public IDictionary<string, CollectionRuleTriggerOptions> Triggers { get; set; } = new Dictionary<string, CollectionRuleTriggerOptions>();

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectionRuleOptions_Actions))]
        public IDictionary<string, CollectionRuleActionOptions> Actions { get; set; } = new Dictionary<string, CollectionRuleActionOptions>();

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectionRuleOptions_Limits))]
        public IDictionary<string, CollectionRuleLimitsOptions> Limits { get; set; } = new Dictionary<string, CollectionRuleLimitsOptions>();
    }
}
