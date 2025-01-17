// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Validation
{
    public class IntegerOrHexStringAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (!(value is string stringValue))
            {
                return new ValidationResult(Strings.ErrorMessage_ValueNotString);
            }
            else if (!TryParse(stringValue, out _, out string? error))
            {
                return new ValidationResult(error);
            }
            return ValidationResult.Success;
        }

        public static bool TryParse(string? value, out long result, [NotNullWhen(false)] out string? error)
        {
            result = 0;
            error = null;

            if (string.IsNullOrWhiteSpace(value))
            {
                error = Strings.ErrorMessage_ValueEmptyNullWhitespace;
                return false;
            }
            else if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                // AllowHexSpecifier requires that the "0x" is removed before attempting to parse.
                // It parses the actual value, not the "0x" syntax prefix.
                if (!long.TryParse(value.AsSpan(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out result))
                {
                    error = string.Format(CultureInfo.InvariantCulture, Strings.ErrorMessage_ValueNotHex, value);
                    return false;
                }
            }
            else
            {
                if (!long.TryParse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out result))
                {
                    error = string.Format(CultureInfo.InvariantCulture, Strings.ErrorMessage_ValueNotInt, value);
                    return false;
                }
            }

            return true;
        }
    }
}
