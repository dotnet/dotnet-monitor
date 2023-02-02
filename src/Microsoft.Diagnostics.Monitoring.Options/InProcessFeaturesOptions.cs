// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Monitoring.Options
{
    /// <summary>
    /// Configuration options for in-process features, ones that execute within each target process.
    /// </summary>
    internal class InProcessFeaturesOptions
    {
        [Experimental]
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_InProcessFeaturesOptions_Enabled))]
        [DefaultValue(InProcessFeaturesOptionsDefaults.Enabled)]
        public bool? Enabled { get; set; }
    }
}
