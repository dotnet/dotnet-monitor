// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    public partial class GlobalCounterOptions
    {
        public const float IntervalMinSeconds = 1;
        public const float IntervalMaxSeconds = 60 * 60 * 24; // One day

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_GlobalCounterOptions_IntervalSeconds))]
        [Range(IntervalMinSeconds, IntervalMaxSeconds)]
        [DefaultValue(GlobalCounterOptionsDefaults.IntervalSeconds)]
        public float? IntervalSeconds { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_MetricsOptions_MaxHistograms))]
        [DefaultValue(GlobalCounterOptionsDefaults.MaxHistograms)]
        [Range(1, int.MaxValue)]
        public int? MaxHistograms { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_MetricsOptions_MaxTimeSeries))]
        [DefaultValue(GlobalCounterOptionsDefaults.MaxTimeSeries)]
        [Range(1, int.MaxValue)]
        public int? MaxTimeSeries { get; set; }

        [Display(
            ResourceType = typeof(OptionsDisplayStrings),
            Description = nameof(OptionsDisplayStrings.DisplayAttributeDescription_GlobalCounterOptions_Providers))]
        public System.Collections.Generic.IDictionary<string, GlobalProviderOptions>? Providers { get; set; } = new Dictionary<string, GlobalProviderOptions>(StringComparer.OrdinalIgnoreCase);
    }

    public class GlobalProviderOptions
    {
        [Range(GlobalCounterOptions.IntervalMinSeconds, GlobalCounterOptions.IntervalMaxSeconds)]
        public float? IntervalSeconds { get; set; }
    }

    [OptionsValidator]
    partial class GlobalProviderOptionsValidator : IValidateOptions<GlobalProviderOptions>
    {
    }

    partial class GlobalCounterOptions : IValidatableObject
    {
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var providerValidator = validationContext.GetRequiredService<IValidateOptions<GlobalProviderOptions>>();
            var results = new List<ValidationResult>();

            if (Providers != null)
            {
                foreach ((string provider, GlobalProviderOptions options) in Providers)
                {
                    ValidateOptionsResult providerResults = providerValidator.Validate(provider, options);
                    if (providerResults.Failed)
                    {
                        // We prefix the validation error with the provider.
                        results.AddRange(providerResults.Failures.Select(r => new ValidationResult(
                            string.Format(CultureInfo.CurrentCulture, OptionsDisplayStrings.ErrorMessage_NestedProviderValidationError, provider, r))));
                    }
                }
            }

            return results;
        }
    }

    [OptionsValidator]
    partial class GlobalCounterOptionsValidator : IValidateOptions<GlobalCounterOptions>
    {
        private readonly IServiceProvider _serviceProvider;

        public GlobalCounterOptionsValidator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
    }

    internal static class GlobalCounterOptionsExtensions
    {
        public static float GetIntervalSeconds(this GlobalCounterOptions options) =>
            options.IntervalSeconds.GetValueOrDefault(GlobalCounterOptionsDefaults.IntervalSeconds);

        public static int GetMaxHistograms(this GlobalCounterOptions options) =>
            options.MaxHistograms.GetValueOrDefault(GlobalCounterOptionsDefaults.MaxHistograms);

        public static int GetMaxTimeSeries(this GlobalCounterOptions options) =>
            options.MaxTimeSeries.GetValueOrDefault(GlobalCounterOptionsDefaults.MaxTimeSeries);

        public static float GetProviderSpecificInterval(this GlobalCounterOptions options, string providerName) =>
            options.Providers?.TryGetValue(providerName, out GlobalProviderOptions? providerOptions) == true ? providerOptions.IntervalSeconds ?? options.GetIntervalSeconds() : options.GetIntervalSeconds();
    }
}
