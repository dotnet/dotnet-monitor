// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.Diagnostics.Monitoring.WebApi;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options
{
    /// <summary>
    /// Options for describing custom user-defined templates.
    /// </summary>
    internal sealed partial class TemplateOptions
    {
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectionRuleOptions_Filters))]
        public IDictionary<string, ProcessFilterDescriptor>? CollectionRuleFilters { get; set; } = new Dictionary<string, ProcessFilterDescriptor>();

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectionRuleOptions_Trigger))]
        public IDictionary<string, CollectionRuleTriggerOptions>? CollectionRuleTriggers { get; set; } = new Dictionary<string, CollectionRuleTriggerOptions>();

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectionRuleOptions_Actions))]
        public IDictionary<string, CollectionRuleActionOptions>? CollectionRuleActions { get; set; } = new Dictionary<string, CollectionRuleActionOptions>();

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectionRuleOptions_Limits))]
        public IDictionary<string, CollectionRuleLimitsOptions>? CollectionRuleLimits { get; set; } = new Dictionary<string, CollectionRuleLimitsOptions>();
    }
}
