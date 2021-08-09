// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options
{
    partial class CollectionRuleOptions : IValidatableObject
    {
        IEnumerable<ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
        {
            List<ValidationResult> results = new();

            ValidationContext filtersContext = new(Filters, validationContext, validationContext.Items);
            filtersContext.MemberName = nameof(Filters);
            ValidationHelper.TryValidateItems(Filters, filtersContext, results);

            if (null != Trigger)
            {
                ValidationContext triggerContext = new(Trigger, validationContext, validationContext.Items);
                triggerContext.MemberName = nameof(Trigger);
                Validator.TryValidateObject(Trigger, triggerContext, results);
            }

            ValidationContext actionsContext = new(Actions, validationContext, validationContext.Items);
            actionsContext.MemberName = nameof(Actions);
            ValidationHelper.TryValidateItems(Actions, actionsContext, results);

            return results;
        }
    }
}
