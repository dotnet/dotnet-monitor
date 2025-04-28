// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Threading;

#if EXTENSION
namespace Microsoft.Diagnostics.Monitoring.Extension.Common
#else
namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options
#endif
{
    internal static class ValidationHelper
    {
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
                            results.Add(new ValidationResult(failure, [validationContext.MemberName]));
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
#nullable enable

        public static bool TryValidateObject(object options, Type type, ValidationOptions validationOptions, ValidationContext validationContext, ICollection<ValidationResult> results)
        {
            return TryValidateObject(options, type, validationOptions, validationContext, results);
        }

        public static bool TryValidateObject(object options, Type type, ValidationOptions validationOptions, IServiceProvider serviceProvider, List<ValidationResult> results)
        {
            var validationContext = new ValidationContext(options, type.Name, serviceProvider, items: null) {
                MemberName = type.Name
            };
            return TryValidateObject(options, type, validationOptions, validationContext, results);
        }

        public static bool TryValidateObject(object options, Type type, ValidationOptions validationOptions, ValidationContext validationContext, List<ValidationResult> results)
        {
            if (!validationOptions.TryGetValidatableTypeInfo(type, out IValidatableInfo? validatableTypeInfo))
            {
                throw new Exception("No type info found for type " + type.FullName);
            }
            if (validationContext.MemberName is null)
            {
                throw new ArgumentNullException(nameof(validationContext.MemberName));
            }
            var validateContext = new ValidateContext()
            {
                ValidationOptions = validationOptions,
                ValidationContext = validationContext
            };
            validatableTypeInfo.ValidateAsync(options, validateContext, CancellationToken.None).GetAwaiter().GetResult();
            if (validateContext.ValidationErrors is Dictionary<string, string[]> validationErrors)
            {
                foreach (var (name, errors) in validationErrors)
                {
                    foreach (var error in errors)
                    {
                        results.Add(new ValidationResult(error, [name]));
                    }
                }
                return false;
            }
            return true;
        }

        public static void ValidateObject(object options, Type type, ValidationOptions validationOptions, IServiceProvider serviceProvider)
        {
            if (!TryValidateObject(options, type, validationOptions, serviceProvider, new List<ValidationResult>()))
            {
                throw new ValidationException("Validation failed for " + type.FullName);
            }
        }
    }
}
