﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions
{
    /// <summary>
    /// Options for the <see cref="CollectionRules.Actions.LoadProfilerActionFactory.LoadProfilerAction"/> action.
    /// </summary>
    [DebuggerDisplay("LoadProfiler")]
#if SCHEMAGEN
    [NJsonSchema.Annotations.JsonSchemaFlatten]
#endif
    internal sealed record class LoadProfilerOptions : BaseRecordOptions
    {
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_LoadProfilerOptions_Path))]
        [Required]
        public string Path { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_LoadProfilerOptions_Clsid))]
        [Required]
#if !UNITTEST && !SCHEMAGEN
        [RequiredGuid]
#endif
        public Guid Clsid { get; set; }
    }
}
