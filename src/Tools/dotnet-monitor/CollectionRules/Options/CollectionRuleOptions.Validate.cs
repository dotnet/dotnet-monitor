// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options
{
    partial class CollectionRuleOptions : IValidatableObject
    {
        IEnumerable<ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
        {
            List<ValidationResult> results = new();

            // ErrorList is populated by incorrectly using templates - this will be empty if all templates names can be resolved or if templates are not used.
            results.AddRange(ErrorList);

            if (results.Count > 0)
            {
                return results;
            }

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

            var actionNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (CollectionRuleActionOptions option in Actions)
            {
                if (!string.IsNullOrEmpty(option.Name) && !actionNames.Add(option.Name))
                {
                    results.Add(new ValidationResult(
                        string.Format(CultureInfo.CurrentCulture, Strings.ErrorMessage_DuplicateActionName, option.Name),
                        new[] { nameof(option.Name) }));
                }
            }

            return results;
        }
    }
}
