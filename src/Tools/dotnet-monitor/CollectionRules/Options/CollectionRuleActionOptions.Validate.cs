// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options
{
    partial class CollectionRuleActionOptions : IValidatableObject
    {
        IEnumerable<ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
        {
            ICollectionRuleActionOperations actionOperations = validationContext.GetRequiredService<ICollectionRuleActionOperations>();

            List<ValidationResult> results = new();

            if (!string.IsNullOrEmpty(Type))
            {
                actionOperations.TryValidateOptions(Type, Settings, validationContext, results);
            }

            return results;
        }
    }
}
