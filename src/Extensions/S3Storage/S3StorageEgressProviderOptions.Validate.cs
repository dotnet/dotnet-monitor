// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Monitoring.Extension.S3Storage
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
        }
    }
}
