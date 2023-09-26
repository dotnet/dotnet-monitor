// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Monitoring.AzureBlobStorage
{
    internal sealed partial class AzureBlobEgressProviderOptions :
        IValidatableObject
    {
        /// <summary>
        /// Provides extra validation to ensure that either the
        /// <see cref="AccountKey"/>, <see cref="SharedAccessSignature"/>, or <see cref="ManagedIdentityClientId"/> have been set.
        /// </summary>
        IEnumerable<ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
        {
            IList<ValidationResult> results = new List<ValidationResult>();

            // One of the authentication keys/tokens is required
            if (string.IsNullOrEmpty(AccountKey) && string.IsNullOrEmpty(SharedAccessSignature) && string.IsNullOrEmpty(ManagedIdentityClientId) && !(UseWorkloadIdentityFromEnvironment == true))
            {
                results.Add(
                    new ValidationResult(
                        string.Format(
                            Strings.ErrorMessage_CredentialsMissing,
                            nameof(AccountKey),
                            nameof(SharedAccessSignature),
                            nameof(ManagedIdentityClientId),
                            nameof(UseWorkloadIdentityFromEnvironment))));
            }

            return results;
        }
    }
}
