// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions
{
    internal sealed class RequiredGuidAttribute :
        ValidationAttribute
    {
        public override string FormatErrorMessage(string name)
        {
            return string.Format(
                        CultureInfo.InvariantCulture,
                        Strings.ErrorMessage_GuidRequired,
                        name);
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (!(value is Guid))
            {
                return new ValidationResult(
                    FormatErrorMessage(validationContext.DisplayName));
            }

            Guid guidVal = (Guid)value;

            if (guidVal == Guid.Empty)
            {
                return new ValidationResult(
                    FormatErrorMessage(validationContext.DisplayName));
            }

            return ValidationResult.Success;
        }
    }
}
