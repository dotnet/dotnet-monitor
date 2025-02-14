// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Monitoring.Options
{

    public class InfoConfigurator
    {
        private readonly IOptions<CallStacksOptions> _callStacksOptions;
        private readonly IOptions<ParameterCapturingOptions> _parameterCapturingOptions;
        private readonly IOptions<ExceptionsOptions> _exceptionsOptions;
        private readonly IOptionsMonitor<MetricsOptions> _metricsOptions;

        public InfoConfigurator(IServiceProvider serviceProvider)
        {
            _callStacksOptions = serviceProvider.GetRequiredService<IOptions<CallStacksOptions>>();
            _parameterCapturingOptions = serviceProvider.GetRequiredService<IOptions<ParameterCapturingOptions>>();
            _exceptionsOptions = serviceProvider.GetRequiredService<IOptions<ExceptionsOptions>>();
            _metricsOptions = serviceProvider.GetRequiredService<IOptionsMonitor<MetricsOptions>>();
        }

        public List<string> GetFeatureAvailability()
        {
            List<string> enabledFeatures = [];

            if (_callStacksOptions.Value.Enabled == true)
            {
                enabledFeatures.Add("CallStacks");
            }

            if (_parameterCapturingOptions.Value.Enabled == true)
            {
                enabledFeatures.Add("ParameterCapturing");
            }

            if (_exceptionsOptions.Value.Enabled == true)
            {
                enabledFeatures.Add("Exceptions");
            }

            if (_metricsOptions.CurrentValue.Enabled == true)
            {
                enabledFeatures.Add("Metrics");
            }

            return enabledFeatures;
        }
    }
}
