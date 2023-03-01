// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers
{
    partial class SystemDiagnosticsMetricsOptions : IValidatableObject
    {
        IEnumerable<ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
        {
            List<ValidationResult> results = new();

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

            if (!string.IsNullOrEmpty(ProviderName) && !string.IsNullOrEmpty(MeterName))
            {
                // Both ProviderName and MeterName cannot be specified simultaneously.
                results.Add(new ValidationResult(
                    string.Format(
                        Strings.ErrorMessage_TwoFieldsCannotBeSpecified,
                        nameof(MeterName),
                        nameof(ProviderName))));
            }
            else if (string.IsNullOrEmpty(ProviderName) && string.IsNullOrEmpty(MeterName))
            {
                // Both ProviderName and MeterName cannot be empty.
                results.Add(new ValidationResult(
                    string.Format(
                        Strings.ErrorMessage_FieldMissing,
                        nameof(MeterName))));
            }

            if (!string.IsNullOrEmpty(CounterName) && !string.IsNullOrEmpty(InstrumentName))
            {
                // Both CounterName and InstrumentName cannot be specified simultaneously.
                results.Add(new ValidationResult(
                    string.Format(
                        Strings.ErrorMessage_TwoFieldsCannotBeSpecified,
                        nameof(InstrumentName),
                        nameof(CounterName))));
            }
            else if (string.IsNullOrEmpty(CounterName) && string.IsNullOrEmpty(InstrumentName))
            {
                // Both CounterName and InstrumentName cannot be empty.
                results.Add(new ValidationResult(
                    string.Format(
                        Strings.ErrorMessage_FieldMissing,
                        nameof(InstrumentName))));
            }

            return results;
        }
    }
}
