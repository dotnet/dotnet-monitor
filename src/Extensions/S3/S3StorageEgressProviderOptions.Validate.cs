// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Monitoring.S3
{
    /// <summary>
    /// Egress provider options for S3 storage.
    /// </summary>
    internal sealed partial class S3StorageEgressProviderOptions : IValidatableObject
    {
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!string.IsNullOrEmpty(AccessKeyId) && string.IsNullOrEmpty(SecretAccessKey))
                yield return new ValidationResult(Strings.ErrorMessage_EgressS3FailedMissingSecrets);
            if (GeneratePreSignedUrl && !PreSignedUrlExpiry.HasValue)
                yield return new ValidationResult(string.Format(Strings.ErrorMessage_EgressS3FailedMissingOption, nameof(PreSignedUrlExpiry)));
        }
    }
}
