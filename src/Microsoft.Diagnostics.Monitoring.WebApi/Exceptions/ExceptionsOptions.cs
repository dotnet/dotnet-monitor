﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Exceptions
{
    internal sealed class ExceptionsOptions :
        IInProcessFeatureOptions
    {
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_ExceptionsOptions_Enabled))]
        [DefaultValue(ExceptionsOptionsDefaults.Enabled)]
        public bool? Enabled { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_ExceptionsOptions_TopLevelLimit))]
        [Range(1, int.MaxValue)]
        [DefaultValue(ExceptionsOptionsDefaults.TopLevelLimit)]
        public int? TopLevelLimit { get; set; }

        public ExceptionsConfiguration Filters { get; set; }
    }
}
