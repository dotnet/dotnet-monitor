// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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

            // A session token is only meaningful alongside a temporary access key id / secret access key pair.
            if (!string.IsNullOrEmpty(SessionToken) && (string.IsNullOrEmpty(AccessKeyId) || string.IsNullOrEmpty(SecretAccessKey)))
                yield return new ValidationResult(Strings.ErrorMessage_EgressS3FailedMissingSessionCredentials);

            // An unsigned payload is only tamper-protected by the transport, so the AWS SDK refuses to
            // send one over plain HTTP. Uri parsing (rather than a string prefix check) tolerates the
            // leading whitespace and casing that environment-injected values can carry. This check only
            // sees the configured Endpoint: when it is empty the SDK may still resolve an endpoint from
            // the environment (AWS_ENDPOINT_URL_S3 / AWS_ENDPOINT_URL); a non-HTTPS endpoint from that
            // path escapes validation here and is rejected by the SDK's signer at upload time instead.
            if (DisablePayloadSigning
                && !string.IsNullOrEmpty(Endpoint)
                && !(Uri.TryCreate(Endpoint, UriKind.Absolute, out Uri? endpointUri) && endpointUri.Scheme == Uri.UriSchemeHttps))
                yield return new ValidationResult(Strings.ErrorMessage_EgressS3FailedInsecurePayloadSigningOptOut);
        }
    }
}
