// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Monitoring.Options
{
    public sealed class ExceptionsOptions :
        IInProcessFeatureOptions
    {
        [Display(
            Name = nameof(Enabled),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_ExceptionsOptions_Enabled))]
        [DefaultValue(ExceptionsOptionsDefaults.Enabled)]
        public bool? Enabled { get; set; }

        [Display(
            Name = nameof(TopLevelLimit),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_ExceptionsOptions_TopLevelLimit))]
        [Range(1, int.MaxValue)]
        [DefaultValue(ExceptionsOptionsDefaults.TopLevelLimit)]
        public int? TopLevelLimit { get; set; }

        [Display(
            Name = nameof(CollectionFilters),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_ExceptionsOptions_CollectionFilters))]
        public ExceptionsConfiguration? CollectionFilters { get; set; }
    }
}
