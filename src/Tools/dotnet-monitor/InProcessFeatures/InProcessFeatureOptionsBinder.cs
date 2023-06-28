// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal static class InProcessFeatureOptionsBinder
    {
        public static void BindEnabled(IInProcessFeatureOptions options, string configurationKey, IConfiguration configuration, bool enabledByDefault)
        {
            bool? inProcessFeaturesEnabled = null;
            IConfigurationSection inProcessFeaturesSection = configuration.GetSection(ConfigurationKeys.InProcessFeatures_Enabled);
            if (!string.IsNullOrEmpty(inProcessFeaturesSection.Value))
            {
                inProcessFeaturesEnabled = inProcessFeaturesSection.Get<bool>();
            }

            // If in-process features are disabled, then disable the individual feature
            if (!inProcessFeaturesEnabled.GetValueOrDefault(true))
            {
                options.Enabled = false;
            }

            // If feature has explicit enablement value, then preserve it.
            if (options.Enabled.HasValue)
                return;

            // Check if the feature is enabled or disabled from the section e.g. InProcessFeatures:CallStacks = true
            IConfigurationSection featureSection = configuration.GetSection(configurationKey);
            if (!string.IsNullOrEmpty(featureSection.Value))
            {
                options.Enabled = featureSection.Get<bool>();
                return;
            }

            // If in-process features are enabled, fallback to the default enablement.
            if (inProcessFeaturesEnabled.GetValueOrDefault(false))
            {
                options.Enabled = enabledByDefault;
                return;
            }

            // This feature and in-process features are not enabled; thus feature is disabled.
            options.Enabled = false;
        }
    }
}
