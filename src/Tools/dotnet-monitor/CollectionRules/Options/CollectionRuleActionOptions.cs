// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.Diagnostics.Monitoring.WebApi;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options
{
    /// <summary>
    /// Options for describing the type of action to execute and the settings to pass to that action.
    /// </summary>
    [DebuggerDisplay("Action: Type = {Type}")]
    internal sealed partial class CollectionRuleActionOptions
    {
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectionRuleActionOptions_Name))]
        public string? Name { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectionRuleActionOptions_Type))]
        [Required]
        public string Type { get; set; } = string.Empty;

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectionRuleActionOptions_Settings))]
        public object? Settings { get; internal set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectionRuleActionOptions_WaitForCompletion))]
        [DefaultValue(CollectionRuleActionOptionsDefaults.WaitForCompletion)]
        public bool? WaitForCompletion { get; set; }
    }
}
