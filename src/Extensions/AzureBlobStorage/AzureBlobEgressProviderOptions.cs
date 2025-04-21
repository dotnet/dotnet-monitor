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
            Name = nameof(AccountUri),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AzureBlobEgressProviderOptions_AccountUri))]
        [Required]
        public Uri AccountUri { get; set; }

        [Display(
            Name = nameof(AccountKey),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AzureBlobEgressProviderOptions_AccountKey))]
        public string AccountKey { get; set; }

        [Display(
            Name = nameof(AccountKeyName),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AzureBlobEgressProviderOptions_AccountKeyName))]
        public string AccountKeyName { get; set; }

        [Display(
            Name = nameof(SharedAccessSignature),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AzureBlobEgressProviderOptions_SharedAccessSignature))]
        public string SharedAccessSignature { get; set; }

        [Display(
            Name = nameof(SharedAccessSignatureName),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AzureBlobEgressProviderOptions_SharedAccessSignatureName))]
        public string SharedAccessSignatureName { get; set; }

        [Display(
            Name = nameof(ManagedIdentityClientId),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AzureBlobEgressProviderOptions_ManagedIdentityClientId))]
        public string ManagedIdentityClientId { get; set; }

        [Display(
            Name = nameof(UseWorkloadIdentityFromEnvironment),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AzureBlobEgressProviderOptions_UseWorkloadIdentityFromEnvironment))]
        public bool? UseWorkloadIdentityFromEnvironment { get; set; }

        [Display(
            Name = nameof(ContainerName),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AzureBlobEgressProviderOptions_ContainerName))]
        [Required]
        public string ContainerName { get; set; }

        [Display(
            Name = nameof(BlobPrefix),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AzureBlobEgressProviderOptions_BlobPrefix))]
        public string BlobPrefix { get; set; }

        [Display(
            Name = nameof(CopyBufferSize),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CommonEgressProviderOptions_CopyBufferSize))]
        [Range(1, int.MaxValue)]
        public int? CopyBufferSize { get; set; }

        [Display(
            Name = nameof(QueueName),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AzureBlobEgressProviderOptions_QueueName))]
        public string QueueName { get; set; }

        [Display(
            Name = nameof(QueueAccountUri),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AzureBlobEgressProviderOptions_QueueAccountUri))]
        public Uri QueueAccountUri { get; set; }

        [Display(
            Name = nameof(QueueSharedAccessSignature),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AzureBlobEgressProviderOptions_QueueSharedAccessSignature))]
        public string QueueSharedAccessSignature { get; set; }

        [Display(
            Name = nameof(QueueSharedAccessSignatureName),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AzureBlobEgressProviderOptions_QueueSharedAccessSignatureName))]
        public string QueueSharedAccessSignatureName { get; set; }

        [Display(
            Name = nameof(Metadata),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_AzureBlobEgressProviderOptions_Metadata))]
        public IDictionary<string, string> Metadata { get; set; }
            = new Dictionary<string, string>(0);
    }
}
