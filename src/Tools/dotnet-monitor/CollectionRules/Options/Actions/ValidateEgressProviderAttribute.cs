// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions
{
    internal sealed class ValidateEgressProviderAttribute :
        ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (!(value is string))
            {
                return new ValidationResult(
                    FormatErrorMessage(validationContext.DisplayName));
            }

            string egressProvider = (string)value;

            IEgressService egressService = validationContext.GetRequiredService<IEgressService>();
            try
            {
                egressService.ValidateProviderExists(egressProvider);
            }
            catch (Exception)
            {
                return new ValidationResult(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        Strings.ErrorMessage_EgressProviderDoesNotExist,
                        egressProvider));
            }

            return ValidationResult.Success;
        }
    }
}
