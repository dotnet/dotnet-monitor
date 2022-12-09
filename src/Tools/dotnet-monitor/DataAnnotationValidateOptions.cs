// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class DataAnnotationValidateOptions<TOptions> :
        IValidateOptions<TOptions>
        where TOptions : class
    {
        private readonly IServiceProvider _serviceProvider;

        public DataAnnotationValidateOptions(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public ValidateOptionsResult Validate(string name, TOptions options)
        {
            ValidationContext validationContext = new(options, _serviceProvider, null);
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
