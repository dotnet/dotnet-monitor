// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers.EventCounterShortcuts
{
    partial interface IEventCounterShortcuts : IValidatableObject
    {
        IEnumerable<ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
        {
            List<ValidationResult> results = new();

            if (LessThan.HasValue && GreaterThan.HasValue && GreaterThan.Value > LessThan.Value)
            {
                // The GreaterThan must be less than or equal to LessThan if both are specified.
                results.Add(new ValidationResult(
                    string.Format(
                        Strings.ErrorMessage_FieldMustBeLessThanOtherField,
                        nameof(GreaterThan),
                        nameof(LessThan))));
            }

            return results;
        }
    }
}
