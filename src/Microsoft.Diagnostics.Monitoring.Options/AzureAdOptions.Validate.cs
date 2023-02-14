// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed partial class AzureAdOptions :
        IValidatableObject
    {
        IEnumerable<ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
        {
            IList<ValidationResult> results = new List<ValidationResult>();

            // At least either a required role or scope must be specified.
            if (string.IsNullOrEmpty(RequiredRole) && string.IsNullOrEmpty(RequiredScope))
            {
                results.Add(
                    new ValidationResult(
                        string.Format(
                            OptionsDisplayStrings.ErrorMessage_NeitherFieldSpecified,
                            nameof(RequiredRole),
                            nameof(RequiredScope))));
            }

            return results;
        }
    }
}
