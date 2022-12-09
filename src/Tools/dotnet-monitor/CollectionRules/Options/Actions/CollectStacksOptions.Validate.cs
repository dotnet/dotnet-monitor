// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions
{
    partial record class CollectStacksOptions :
        IValidatableObject
    {
        IEnumerable<ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            IInProcessFeatures features = validationContext.GetRequiredService<IInProcessFeatures>();

            if (!features.IsCallStacksEnabled)
            {
                results.Add(new ValidationResult(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        Strings.ErrorMessage_DisabledFeature,
                        nameof(CollectionRules.Actions.CollectStacksAction))));
            }

            return results;
        }
    }
}
