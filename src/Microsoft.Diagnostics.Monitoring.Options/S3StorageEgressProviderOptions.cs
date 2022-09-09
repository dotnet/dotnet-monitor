// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress.S3
{
    /// <summary>
    /// Egress provider options for file system egress.
    /// </summary>
    internal sealed class S3StorageEgressProviderOptions :
        IEgressProviderCommonOptions, IValidatableObject
    {
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_S3StorageEgressProviderOptions_Endpoint))]
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
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_S3StorageEgressProviderOptions_AccessKeyId))]
        public string AccessKeyId { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_S3StorageEgressProviderOptions_SecretsAccessKeyFile))]
        public string SecretsAccessKeyFile { get; set; }
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_S3StorageEgressProviderOptions_SecretAccessKey))]
        public string SecretAccessKey { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_S3StorageEgressProviderOptions_AWSProfileName))]
        public string AwsProfileName { get; set; }
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_S3StorageEgressProviderOptions_AWSProfilePath))]
        public string AwsProfileFilePath { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_S3StorageEgressProviderOptions_GeneratePreSignedUrl))]
        public bool GeneratePresSignedUrl { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_S3StorageEgressProviderOptions_PreSignedUrlExpiryInMinutes))]
        public int PreSignedUrlExpiryInMinutes { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_S3StorageEgressProviderOptions_ForcePathStyle))]
        public bool ForcePathStyle { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CommonEgressProviderOptions_CopyBufferSize))]
        [Range(1, int.MaxValue)]
        public int? CopyBufferSize => 1024 * 1024 * 50;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!string.IsNullOrEmpty(AccessKeyId) && string.IsNullOrEmpty(SecretsAccessKeyFile) && string.IsNullOrEmpty(SecretAccessKey))
                yield return new ValidationResult(OptionsDisplayStrings.ErrorMessage_EgressS3FailedMissingSecrets);

            if (string.IsNullOrEmpty(BucketName))
                yield return new ValidationResult(string.Format(OptionsDisplayStrings.ErrorMessage_EgressS3FailedMissingOption, nameof(BucketName)));

            if (GeneratePresSignedUrl && PreSignedUrlExpiryInMinutes <= 0)
                yield return new ValidationResult(string.Format(OptionsDisplayStrings.ErrorMessage_EgressS3FailedMissingOption, nameof(PreSignedUrlExpiryInMinutes)));
        }
    }
}
