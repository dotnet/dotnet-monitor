// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress.AzureBlob
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
            if (string.IsNullOrEmpty(AccountKey) && string.IsNullOrEmpty(SharedAccessSignature) && string.IsNullOrEmpty(ManagedIdentityClientId))
            {
                results.Add(
                    new ValidationResult("TEMPORARY"));
                /*
                results.Add(
                    new ValidationResult(
                        string.Format(
                            OptionsDisplayStrings.ErrorMessage_CredentialsMissing,
                            nameof(AccountKey),
                            nameof(SharedAccessSignature),
                            nameof(ManagedIdentityClientId))));
                */
            }

            return results;
        }
    }
}
