// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Monitoring.Options
{
    internal sealed class CallStacksOptions :
        IInProcessFeatureOptions
    {
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CallStacksOptions_Enabled))]
        [DefaultValue(CallStacksOptionsDefaults.Enabled)]
        public bool? Enabled { get; set; }
    }
}
