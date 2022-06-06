// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress.AzureBlob
{
    /// <summary>
    /// Egress provider options for Azure blob storage.
    /// </summary>
    internal sealed partial class AzureBlobEgressProviderOptions :
        IEgressProviderCommonOptions
    {
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AzureBlobEgressProviderOptions_AccountUri))]
        [Required]
        public Uri AccountUri { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AzureBlobEgressProviderOptions_AccountKey))]
        public string AccountKey { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AzureBlobEgressProviderOptions_AccountKeyName))]
        public string AccountKeyName { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AzureBlobEgressProviderOptions_SharedAccessSignature))]
        public string SharedAccessSignature { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AzureBlobEgressProviderOptions_SharedAccessSignatureName))]
        public string SharedAccessSignatureName { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AzureBlobEgressProviderOptions_ManagedIdentityClientId))]
        public string ManagedIdentityClientId { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AzureBlobEgressProviderOptions_ContainerName))]
        [Required]
        public string ContainerName { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AzureBlobEgressProviderOptions_BlobPrefix))]
        public string BlobPrefix { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CommonEgressProviderOptions_CopyBufferSize))]
        public int? CopyBufferSize { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AzureBlobEgressProviderOptions_QueueName))]
        public string QueueName { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AzureBlobEgressProviderOptions_QueueAccountUri))]
        public Uri QueueAccountUri { get; set; }
    }
}
