// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options
{
    internal static class ValidationHelper
    {
        public static void TryValidateItems(IEnumerable<object> items, ValidationContext validationContext, ICollection<ValidationResult> results)
        {
            int index = 0;
            foreach (object item in items)
            {
                ValidationContext itemContext = new(item, validationContext, validationContext.Items);
                itemContext.MemberName = validationContext.MemberName + "[" + index.ToString() + "]";

                Validator.TryValidateObject(item, itemContext, results);

                index++;
            }
        }

#nullable disable
        public static bool TryValidateOptions(Type optionsType, object options, ValidationContext validationContext, ICollection<ValidationResult> results)
        {
            RequiredAttribute requiredAttribute = new();
            ValidationResult requiredResult = requiredAttribute.GetValidationResult(options, validationContext);
            if (requiredResult == ValidationResult.Success)
            {
                Type validateOptionsType = typeof(IValidateOptions<>).MakeGenericType(optionsType);
                MethodInfo validateMethod = validateOptionsType.GetMethod(nameof(IValidateOptions<object>.Validate));

                bool hasFailedResults = false;
                IEnumerable<object> validateOptionsImpls = validationContext.GetServices(validateOptionsType);
                foreach (object validateOptionsImpl in validateOptionsImpls)
                {
                    ValidateOptionsResult validateResult = (ValidateOptionsResult)validateMethod.Invoke(validateOptionsImpl, new object[] { null, options });
                    if (validateResult.Failed)
                    {
                        foreach (string failure in validateResult.Failures)
                        {
                            results.Add(new ValidationResult(failure));
                            hasFailedResults = true;
                        }
                    }
                }

                return hasFailedResults;
            }
            else
            {
                results.Add(requiredResult);
                return false;
            }
        }
#nullable restore
    }
}
