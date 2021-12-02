// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions
{
    /// <summary>
    /// Options for the <see cref="CollectionRules.Actions.LoadProfilerAction"/> action.
    /// </summary>
    [DebuggerDisplay("LoadProfiler")]
    internal sealed partial class LoadProfilerOptions
    {
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_LoadProfilerOptions_Path))]
        [Required]
        public string Path { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_LoadProfilerOptions_ProfilerGuid))]
        [Required]
        public Guid ProfilerGuid { get; set; } = Guid.Empty;
    }
}
