// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Monitoring.AzureBlobStorage
{
    /// <summary>
    /// Egress provider options for Azure blob storage.
    /// </summary>
    internal sealed partial class AzureBlobEgressProviderOptions
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
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AzureBlobEgressProviderOptions_UseWorkloadIdentityFromEnvironment))]
        public bool? UseWorkloadIdentityFromEnvironment { get; set; }

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
        [Range(1, int.MaxValue)]
        public int? CopyBufferSize { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AzureBlobEgressProviderOptions_QueueName))]
        public string QueueName { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AzureBlobEgressProviderOptions_QueueAccountUri))]
        public Uri QueueAccountUri { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AzureBlobEgressProviderOptions_QueueSharedAccessSignature))]
        public string QueueSharedAccessSignature { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AzureBlobEgressProviderOptions_QueueSharedAccessSignatureName))]
        public string QueueSharedAccessSignatureName { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AzureBlobEgressProviderOptions_Metadata))]
        public IDictionary<string, string> Metadata { get; set; }
            = new Dictionary<string, string>(0);
    }
}
