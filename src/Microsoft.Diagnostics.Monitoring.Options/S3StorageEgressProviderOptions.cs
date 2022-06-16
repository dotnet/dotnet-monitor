// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress.S3
{
    /// <summary>
    /// Egress provider options for file system egress.
    /// </summary>
    internal sealed class S3StorageEgressProviderOptions :
        IEgressProviderCommonOptions
    {
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_S3StorageEgressProviderOptions_Endpoint))]
        [Required]
        public string Endpoint { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_S3StorageEgressProviderOptions_BucketName))]
        public string BucketName { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_S3StorageEgressProviderOptions_RegionName))]
        public string RegionName { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_S3StorageEgressProviderOptions_UserName))]
        public string UserName { get; set; }
        
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_S3StorageEgressProviderOptions_SecretsFile))]
        public string SecretsFile { get; set; }
        
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_S3StorageEgressProviderOptions_Password))]
        public string Password { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CommonEgressProviderOptions_CopyBufferSize))]
        [Range(1, int.MaxValue)]
        public int? CopyBufferSize => 1024 * 1024 * 50;
    }
}
