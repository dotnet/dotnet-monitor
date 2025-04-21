// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Diagnostics.Monitoring.Extension.S3Storage
{
    /// <summary>
    /// Egress provider options for S3 storage.
    /// </summary>
    internal sealed partial class S3StorageEgressProviderOptions
    {
        [Display(
            Name = nameof(Endpoint),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_S3StorageEgressProviderOptions_Endpoint))]
        public string? Endpoint { get; set; }

        [Display(
            Name = nameof(BucketName),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_S3StorageEgressProviderOptions_BucketName))]
        [Required(AllowEmptyStrings = false)]
        public string BucketName { get; set; } = string.Empty;

        [Display(
            Name = nameof(RegionName),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_S3StorageEgressProviderOptions_RegionName))]
        public string? RegionName { get; set; }

        [Display(
            Name = nameof(AccessKeyId),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_S3StorageEgressProviderOptions_AccessKeyId))]
        public string? AccessKeyId { get; set; }

        [Display(
            Name = nameof(SecretAccessKey),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_S3StorageEgressProviderOptions_SecretAccessKey))]
        public string? SecretAccessKey { get; set; }

        [Display(
            Name = nameof(AwsProfileName),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_S3StorageEgressProviderOptions_AWSProfileName))]
        public string? AwsProfileName { get; set; }

        [Display(
            Name = nameof(AwsProfilePath),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_S3StorageEgressProviderOptions_AWSProfilePath))]
        public string? AwsProfilePath { get; set; }

        [Display(
            Name = nameof(PreSignedUrlExpiry),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_S3StorageEgressProviderOptions_PreSignedUrlExpiry))]
        [Range(typeof(TimeSpan), "00:01:00", "1.00:00:00")]
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Addressed by DynamicDependency on ValidationHelper.TryValidateOptions method")]
        public TimeSpan? PreSignedUrlExpiry { get; set; }

        [Display(
            Name = nameof(ForcePathStyle),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_S3StorageEgressProviderOptions_ForcePathStyle))]
        public bool ForcePathStyle { get; set; }

        [Display(
            Name = nameof(CopyBufferSize),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CommonEgressProviderOptions_CopyBufferSize))]
        [Range(1, int.MaxValue)]
        public int? CopyBufferSize { get; set; }

        [Display(
            Name = nameof(UseKmsEncryption),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_S3StorageEgressProviderOptions_UseKmsEncryption))]
        public bool UseKmsEncryption { get; set; }

        [Display(
            Name = nameof(KmsEncryptionKey),
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_S3StorageEgressProviderOptions_KmsEncryptionKey))]
        public string? KmsEncryptionKey { get; set; }
    }
}
