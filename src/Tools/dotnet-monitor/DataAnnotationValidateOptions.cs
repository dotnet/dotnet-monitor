// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

#if EXTENSION
namespace Microsoft.Diagnostics.Monitoring.Extension.Common
#else
namespace Microsoft.Diagnostics.Tools.Monitor
#endif
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

        public ValidateOptionsResult Validate(string? name, TOptions options)
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
#nullable disable
                        failures.Add(result.ErrorMessage);
#nullable restore
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
