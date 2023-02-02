// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress.S3
{
    /// <summary>
    /// Egress provider options for S3 storage.
    /// </summary>
    internal sealed partial class S3StorageEgressProviderOptions : IValidatableObject
    {
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!string.IsNullOrEmpty(AccessKeyId) && string.IsNullOrEmpty(SecretAccessKey))
                yield return new ValidationResult(OptionsDisplayStrings.ErrorMessage_EgressS3FailedMissingSecrets);
            if (GeneratePreSignedUrl && !PreSignedUrlExpiry.HasValue)
                yield return new ValidationResult(string.Format(OptionsDisplayStrings.ErrorMessage_EgressS3FailedMissingOption, nameof(PreSignedUrlExpiry)));
        }
    }
}
