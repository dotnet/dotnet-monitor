// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress.Configuration
{
    /// <summary>
    /// Validates the settings within a <typeparamref name="TOptions"/> instance
    /// using data annotation validation.
    /// </summary>
    internal sealed class EgressProviderValidateOptions<TOptions> :
        IValidateOptions<TOptions> where TOptions : class
    {
        public ValidateOptionsResult Validate(string name, TOptions options)
        {
            ValidationContext validationContext = new ValidationContext(options);
            ICollection<ValidationResult> results = new Collection<ValidationResult>();
            if (!Validator.TryValidateObject(options, validationContext, results, validateAllProperties: true))
            {
                IList<string> failures = new List<string>();
                foreach (ValidationResult result in results)
                {
                    if (ValidationResult.Success != result)
                    {
                        failures.Add(result.ErrorMessage);
                    }
                }

                if (failures.Count > 0)
                {
                    return ValidateOptionsResult.Fail(failures);
                }
            }

            return ValidateOptionsResult.Success;
        }
    }
}
