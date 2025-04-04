﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Http.Validation;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;

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
        private readonly ValidationOptions _validationOptions;

        public DataAnnotationValidateOptions(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _validationOptions = serviceProvider.GetRequiredService<IOptions<ValidationOptions>>().Value;
        }

        public ValidateOptionsResult Validate(string? name, TOptions options)
        {
            var results = new List<ValidationResult>();
            if (!ValidationHelper.TryValidateObject(options, typeof(TOptions), _validationOptions, results))
            {
                IList<string> failures = new List<string>();
                foreach (ValidationResult result in results)
                {
                    if (result.MemberNames is IEnumerable<string> memberNames)
                    {
                        foreach (string memberName in memberNames)
                        {
                            failures.Add($"{memberName}: {result.ErrorMessage}");
                        }
                    }
                    else
                    {
                        if (result.ErrorMessage is null)
                        {
                            throw new ArgumentNullException(nameof(result.ErrorMessage));
                        }
                        failures.Add(result.ErrorMessage);
                    }
                }

                return ValidateOptionsResult.Fail(failures);
            }

            return ValidateOptionsResult.Success;
        }
    }
}
