// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    public class StorageOptions
    {
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_StorageOptions_DefaultSharedPath))]
        public string? DefaultSharedPath { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_StorageOptions_DumpTempFolder))]
        public string? DumpTempFolder { get; set; }

        [Options.Experimental]
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_StorageOptions_SharedLibraryPath))]
        public string? SharedLibraryPath { get; set; }

        internal bool Configured
        {
            [MemberNotNullWhen(true, nameof(DumpTempFolder), nameof(SharedLibraryPath))]
            get;
            set;
        }
    }
}
