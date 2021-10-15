// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions
{
    /// <summary>
    /// Options for the Execute action.
    /// </summary>
    [DebuggerDisplay("Execute: Path = {Path}")]
    internal sealed class ExecuteOptions : ICloneable
    {
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_ExecuteOptions_Path))]
        [Required]
        public string Path { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_ExecuteOptions_Arguments))]
        [ActionOptionsDependencyProperty]
        public string Arguments { get; set; }

        [DefaultValue(ExecuteOptionsDefaults.IgnoreExitCode)]
        public bool? IgnoreExitCode { get; set; }

        public object Clone() => MemberwiseClone();
    }
}
