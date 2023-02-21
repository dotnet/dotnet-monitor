// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed partial class AuthenticationOptions :
        IValidatableObject
    {
        IEnumerable<ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
        {
            IList<ValidationResult> results = new List<ValidationResult>();

            // At most only one authentication configuration can be specified.
            if (MonitorApiKey != null && AzureAd != null)
            {
                results.Add(
                    new ValidationResult(
                        string.Format(
                            OptionsDisplayStrings.ErrorMessage_MultipleAuthenticationModesSpecified)));
            }

            return results;
        }
    }
}
