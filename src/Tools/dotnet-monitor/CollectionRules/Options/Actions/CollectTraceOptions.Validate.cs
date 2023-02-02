// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Diagnostics.Monitoring.WebApi.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;

#if UNITTEST
namespace Microsoft.Diagnostics.Monitoring.TestCommon.Options
#else
namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions
#endif
{
    partial record class CollectTraceOptions :
        IValidatableObject
    {
        IEnumerable<ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
        {
            List<ValidationResult> results = new();

            bool hasProfile = Profile.HasValue;
            bool hasProviders = null != Providers && Providers.Any();
            bool hasStoppingEvent = null != StoppingEvent;

            if (hasProfile)
            {
                if (hasStoppingEvent)
                {
                    results.Add(new ValidationResult(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            Strings.ErrorMessage_TwoFieldsCannotBeSpecified,
                            nameof(Profile),
                            nameof(StoppingEvent))));
                }

                if (hasProviders)
                {
                    // Both Profile and Providers cannot be specified at the same time, otherwise
                    // cannot determine whether to use providers from the profile or the custom
                    // specified providers.
                    results.Add(new ValidationResult(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            Strings.ErrorMessage_TwoFieldsCannotBeSpecified,
                            nameof(Profile),
                            nameof(Providers))));
                }
            }
            else if (hasProviders)
            {
                // Validate that each provider is valid.
                int index = 0;
                foreach (EventPipeProvider provider in Providers)
                {
                    ValidationContext providerContext = new(provider, validationContext, validationContext.Items);
                    providerContext.MemberName = nameof(Providers) + "[" + index.ToString(CultureInfo.InvariantCulture) + "]";

                    Validator.TryValidateObject(provider, providerContext, results, validateAllProperties: true);

                    IOptionsMonitor<GlobalCounterOptions> counterOptions = validationContext.GetRequiredService<IOptionsMonitor<GlobalCounterOptions>>();
                    if (!CounterValidator.ValidateProvider(counterOptions.CurrentValue,
                        provider, out string errorMessage))
                    {
                        results.Add(new ValidationResult(errorMessage, new[] { nameof(EventPipeProvider.Arguments) }));
                    }

                    index++;
                }
            }
            else
            {
                // Either Profile or Providers must be specified
                results.Add(new ValidationResult(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        Strings.ErrorMessage_TwoFieldsMissing,
                        nameof(Profile),
                        nameof(Providers))));
            }

            if (hasStoppingEvent)
            {
                bool hasMatchingStoppingProvider = hasProviders
                    && null != Providers.Find(x => string.Equals(x.Name, StoppingEvent.ProviderName, System.StringComparison.Ordinal));

                if (!hasMatchingStoppingProvider)
                {
                    results.Add(new ValidationResult(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            Strings.ErrorMessage_MissingStoppingEventProvider,
                            nameof(StoppingEvent),
                            StoppingEvent.ProviderName,
                            nameof(Providers))));
                }
            }

            return results;
        }
    }
}
