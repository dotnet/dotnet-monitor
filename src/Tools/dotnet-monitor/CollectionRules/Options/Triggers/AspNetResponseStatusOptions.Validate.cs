// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers
{
    partial class AspNetResponseStatusOptions : IValidatableObject
    {
        IEnumerable<ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
        {
            var collectionRuleDefaultOptions = validationContext.GetService<IOptionsMonitor<CollectionRuleDefaultOptions>>();

            List<ValidationResult> results = new();

            if (null == ResponseCount)
            {
                ResponseCount = collectionRuleDefaultOptions.CurrentValue.ResponseCount;

                if (null == ResponseCount)
                {
                    results.Add(new ValidationResult(Strings.ErrorMessage_NoDefaultResponseCount));
                }
            }

            return results;
        }
    }
}
