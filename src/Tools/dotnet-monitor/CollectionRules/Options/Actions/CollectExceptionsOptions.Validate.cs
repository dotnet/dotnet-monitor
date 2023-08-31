// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions
{
    partial record class CollectExceptionsOptions :
        IValidatableObject
    {
        IEnumerable<ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            IOptions<ExceptionsOptions> exceptionsOptions = validationContext.GetRequiredService<IOptions<ExceptionsOptions>>();

            if (!exceptionsOptions.Value.GetEnabled())
            {
                results.Add(new ValidationResult(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        Strings.ErrorMessage_DisabledFeature,
                        nameof(CollectExceptionsAction))));
            }

            return results;
        }
    }
}
