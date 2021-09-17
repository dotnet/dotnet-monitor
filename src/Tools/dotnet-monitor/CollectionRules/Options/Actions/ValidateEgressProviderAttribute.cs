// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions
{
    internal sealed class ValidateEgressProviderAttribute :
        ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            string egressProvider = (string)value;

            IEgressService egressService = validationContext.GetRequiredService<IEgressService>();
            if (!egressService.CheckProvider(egressProvider))
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
