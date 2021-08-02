﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

#if UNITTEST
namespace Microsoft.Diagnostics.Monitoring.TestCommon.Options
#else
namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options
#endif
{
    /// <summary>
    /// Options for describing the type of action to execute and the settings to pass to that action.
    /// </summary>
    [DebuggerDisplay("Action: Type = {Type}")]
    internal sealed partial class CollectionRuleActionOptions
    {
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectionRuleActionOptions_Type))]
        [Required]
        public string Type { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CollectionRuleActionOptions_Settings))]
        public object Settings { get; internal set; }
    }
}
