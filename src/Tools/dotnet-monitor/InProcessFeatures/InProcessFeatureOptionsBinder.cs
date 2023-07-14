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
            // Get the value of InProcessFeatures:<Feature>:Enabled. This may have already
            // been bound by the default options binding, but this makes it consistent for
            // anything that passes in an unbound options instance and fulfills the complete
            // expected behavior of binding the Enabled property.
            options.Enabled = configuration
                .GetSection(ConfigurationPath.Combine(configurationKey, "Enabled"))
                .Get<bool?>();

            // Tristate value for InProcessFeatures:Enabled
            // - null -> Defer enablement to each individual in-process feature
            // - true -> Enable all in-process features that support default enablement
            // - false -> Unconditionally disable all in-process features
            bool? inProcessFeaturesEnabled = configuration
                .GetSection(ConfigurationKeys.InProcessFeatures_Enabled)
                .Get<bool?>();

            // Check if in-process features should be unconditionally disabled
            if (!inProcessFeaturesEnabled.GetValueOrDefault(true))
            {
                options.Enabled = false;
            }

            // Tristate value for InProcessFeatures:<Feature>:Enabled
            // - null -> Defer enablement to InProcessFeatures:Enabled and the default enablement for the individual feature
            // - true -> Enable the individual feature (because it was not opted out by InProcessFeatures:Enabled)
            // - false -> Unconditionally disable the individual feature
            if (options.Enabled.HasValue)
                return;

            // At this point, the feature does not have an explicit enablement value
            // nor was it unconditionally opted out via InProcessFeatures:Enabled.

            // If in-process features are enabled, fallback to the default enablement for the feature.
            if (inProcessFeaturesEnabled.GetValueOrDefault(false))
            {
                options.Enabled = enabledByDefault;
            }
            else
            {
                // This feature and in-process features are not enabled; thus feature is disabled.
                options.Enabled = false;
            }
        }
    }
}
