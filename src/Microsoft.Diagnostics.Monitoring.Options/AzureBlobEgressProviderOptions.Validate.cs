// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress.AzureBlob
{
    internal sealed partial class AzureBlobEgressProviderOptions :
        IValidatableObject
    {
        /// <summary>
        /// Provides extra validation to ensure that either the
        /// <see cref="AccountKey"/> or the <see cref="SharedAccessSignature"/> have been set.
        /// </summary>
        IEnumerable<ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
        {
            IList<ValidationResult> results = new List<ValidationResult>();

            // One of the authentication keys/tokens is required
            if (string.IsNullOrEmpty(AccountKey) && string.IsNullOrEmpty(SharedAccessSignature))
            {
                results.Add(
                    new ValidationResult(
                        string.Format(
                            OptionsDisplayStrings.ErrorMessage_TwoFieldsMissing,
                            nameof(AccountKey),
                            nameof(SharedAccessSignature))));
            }

            return results;
        }
    }
}
