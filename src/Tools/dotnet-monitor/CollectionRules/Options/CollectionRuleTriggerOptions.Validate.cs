﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options
{
    partial class CollectionRuleTriggerOptions : IValidatableObject
    {
        IEnumerable<ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
        {
            ICollectionRuleTriggerOptionsProvider triggerOptionsProvider = validationContext.GetRequiredService<ICollectionRuleTriggerOptionsProvider>();

            List<ValidationResult> results = new();

            if (!string.IsNullOrEmpty(Type))
            {
                if (triggerOptionsProvider.TryGetOptionsType(Type, out Type optionsType))
                {
                    if (null != optionsType)
                    {
                        ValidationHelper.TryValidateOptions(optionsType, Settings, validationContext, results);
                    }
                }
                else
                {
                    results.Add(new ValidationResult(string.Format(CultureInfo.InvariantCulture, Strings.ErrorMessage_UnknownTriggerType, Type)));
                }
            }

            return results;
        }
    }
}
