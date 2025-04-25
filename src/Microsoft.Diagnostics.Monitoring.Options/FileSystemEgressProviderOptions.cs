// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress.FileSystem
{
    /// <summary>
    /// Egress provider options for file system egress.
    /// </summary>
    internal sealed class FileSystemEgressProviderOptions
    {
        public const int CopyBufferSize_MinValue = 1;
        public const int CopyBufferSize_MaxValue = int.MaxValue;

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_FileSystemEgressProviderOptions_DirectoryPath))]
        [Required]
        public string DirectoryPath { get; set; } = string.Empty;

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_FileSystemEgressProviderOptions_IntermediateDirectoryPath))]
        public string? IntermediateDirectoryPath { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CommonEgressProviderOptions_CopyBufferSize))]
        [Range(CopyBufferSize_MinValue, CopyBufferSize_MaxValue)]
        public int? CopyBufferSize { get; set; }
    }
}
