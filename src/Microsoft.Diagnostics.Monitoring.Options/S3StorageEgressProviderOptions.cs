// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.Diagnostics.Monitoring.WebApi;
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
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_S3StorageEgressProviderOptions_AccountKeyName))]
        public string AccountKeyName { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_S3StorageEgressProviderOptions_SecretsFile))]
        public string SecretsFile { get; set; }
        
        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_S3StorageEgressProviderOptions_AccountKey))]
        public string AccountKey { get; set; }

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
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_CommonEgressProviderOptions_CopyBufferSize))]
        [Range(1, int.MaxValue)]
        public int? CopyBufferSize => 1024 * 1024 * 50;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrEmpty(SecretsFile) && string.IsNullOrEmpty(AccountKey))
                yield return new ValidationResult(OptionsDisplayStrings.ErrorMessage_EgressS3FailedMissingSecrets);

            if (string.IsNullOrEmpty(BucketName))
                yield return new ValidationResult(string.Format(OptionsDisplayStrings.ErrorMessage_EgressS3FailedMissingOption, nameof(BucketName)));

            if (string.IsNullOrEmpty(RegionName))
                yield return new ValidationResult(string.Format(OptionsDisplayStrings.ErrorMessage_EgressS3FailedMissingOption, nameof(RegionName)));

            if(GeneratePresSignedUrl && PreSignedUrlExpiryInMinutes <= 0)
                yield return new ValidationResult(string.Format(OptionsDisplayStrings.ErrorMessage_EgressS3FailedMissingOption, nameof(PreSignedUrlExpiryInMinutes)));
        }
    }
}
