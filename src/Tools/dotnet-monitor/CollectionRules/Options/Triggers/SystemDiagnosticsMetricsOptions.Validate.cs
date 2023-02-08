// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers
{
    partial class SystemDiagnosticsMetricsOptions : IValidatableObject
    {
        IEnumerable<ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
        {
            List<ValidationResult> results = new();

            if (HistogramMode.HasValue && HistogramPercentiles.Count > 0)
            {
                if (GreaterThan.HasValue || LessThan.HasValue)
                {
                    results.Add(new ValidationResult(
                        string.Format(Strings.ErrorMessage_CannotHaveGreaterThanLessThanWithHistogram)));
                }
            }
            else if (HistogramMode.HasValue && !HistogramPercentiles.Any())
            {
                results.Add(new ValidationResult(
                    string.Format(
                        Strings.ErrorMessage_MissingComplementaryField,
                        nameof(HistogramMode),
                        nameof(HistogramPercentiles))));
            }
            else if (!HistogramMode.HasValue && HistogramPercentiles.Count > 0)
            {
                results.Add(new ValidationResult(
                    string.Format(
                        Strings.ErrorMessage_MissingComplementaryField,
                        nameof(HistogramPercentiles),
                        nameof(HistogramMode))));
            }
            else
            {
                if (!GreaterThan.HasValue && !LessThan.HasValue)
                {
                    // Either GreaterThan or LessThan must be specified.
                    results.Add(new ValidationResult(
                        string.Format(
                            Strings.ErrorMessage_TwoFieldsMissing,
                            nameof(GreaterThan),
                            nameof(LessThan))));
                }
                else if (GreaterThan.HasValue && LessThan.HasValue && LessThan.Value < GreaterThan.Value)
                {
                    // The GreaterThan must be lower than LessThan if both are specified.
                    results.Add(new ValidationResult(
                        string.Format(
                            Strings.ErrorMessage_FieldMustBeLessThanOtherField,
                            nameof(GreaterThan),
                            nameof(LessThan))));
                }
            }

            return results;
        }
    }
}
