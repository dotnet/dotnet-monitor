// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.Diagnostics.Monitoring.WebApi;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options
{
    /// <summary>
    /// Options for describing the type of trigger and the settings to pass to that trigger.
    /// </summary>
    [DebuggerDisplay("Trigger: Type = {Type}")]
    internal sealed partial class CollectionRuleTriggerOptions
    {
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectionRuleTriggerOptions_Type))]
        [Required]
        public string Type { get; set; } = string.Empty;

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectionRuleTriggerOptions_Settings))]
        public object? Settings { get; internal set; }
    }
}
