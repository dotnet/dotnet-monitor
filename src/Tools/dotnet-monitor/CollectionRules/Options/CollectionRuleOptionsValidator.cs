// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options
{
    partial class CollectionRuleOptionsValidator : IValidateOptions<CollectionRuleOptions>
    {
        private readonly IServiceProvider _serviceProvider;

        public CollectionRuleOptionsValidator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        private static readonly RequiredAttribute RequiredAttribute = new RequiredAttribute();

        public ValidateOptionsResult Validate(string? name, CollectionRuleOptions options)
        {
            ValidateOptionsResultBuilder? builder = null;
            string displayName = string.IsNullOrEmpty(name) ? "CollectionRuleOptions.Validate" : $"{name}.Validate";
            var context = new ValidationContext(options, displayName, _serviceProvider, null);
            var validationResults = new List<ValidationResult>();
            var validationAttributes = new List<ValidationAttribute>(1);

            context.MemberName = "Validate";
            context.DisplayName = "CollectionRuleOptions.Validate";
            var iValidatableObjectResults = ((IValidatableObject)options).Validate(context);
            if (iValidatableObjectResults != null)
            {
                foreach (var result in iValidatableObjectResults)
                {
                    (builder ??= new()).AddResult(result);
                }
            }

            if (builder is not null)
            {
                return builder.Build();
            }

            context.MemberName = "Trigger";
            context.DisplayName = "CollectionRuleOptions.Trigger";
            validationAttributes.Add(RequiredAttribute);
            if (!Validator.TryValidateValue(options.Trigger, context, validationResults, validationAttributes))
            {
                (builder ??= new()).AddResults(validationResults);
            }

            if (options.Trigger is not null)
            {
                (builder ??= new()).AddResult(new CollectionRuleTriggerOptionsValidator(_serviceProvider).Validate(string.IsNullOrEmpty(name) ? "CollectionRuleOptions.Trigger" : $"{name}.Trigger", options.Trigger));
            }

            if (options.Actions is not null)
            {
                var count = 0;
                foreach (var o in options.Actions)
                {
                    if (o is not null)
                    {
                        (builder ??= new()).AddResult(new CollectionRuleActionOptionsValidator(_serviceProvider).Validate(string.IsNullOrEmpty(name) ? $"CollectionRuleOptions.Actions[{count}]" : $"{name}.Actions[{count}]", o));
                    }
                    else
                    {
                        (builder ??= new()).AddError(string.IsNullOrEmpty(name) ? $"CollectionRuleOptions.Actions[{count}] is null" : $"{name}.Actions[{count}] is null");
                    }
                    count++;
                }
            }

            if (options.Limits is not null)
            {
                (builder ??= new()).AddResult(__CollectionRuleLimitsOptionsValidator__.Validate(string.IsNullOrEmpty(name) ? "CollectionRuleOptions.Limits" : $"{name}.Limits", options.Limits));
            }

            return builder is null ? ValidateOptionsResult.Success : builder.Build();
        }
    }
}
