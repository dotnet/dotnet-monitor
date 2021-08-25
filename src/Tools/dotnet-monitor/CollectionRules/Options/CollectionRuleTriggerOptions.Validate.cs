// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Triggers;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options
{
    partial class CollectionRuleTriggerOptions : IValidatableObject
    {
        IEnumerable<ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
        {
            ICollectionRuleTriggerOperations triggerOperations = validationContext.GetRequiredService<ICollectionRuleTriggerOperations>();

            List<ValidationResult> results = new();

            if (!string.IsNullOrEmpty(Type))
            {
                triggerOperations.TryValidateOptions(Type, Settings, validationContext, results);
            }

            return results;
        }
    }
}
