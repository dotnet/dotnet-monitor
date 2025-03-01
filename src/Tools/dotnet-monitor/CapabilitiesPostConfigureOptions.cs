// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Diagnostics.Monitoring
{
    public class CapabilityPostConfigureOptions :
        IPostConfigureOptions<CallStacksOptions>,
        IPostConfigureOptions<ExceptionsOptions>,
        IPostConfigureOptions<ParameterCapturingOptions>,
        IPostConfigureOptions<MetricsOptions>
    {
        private readonly List<IMonitorCapability> _capabilities;
        private readonly Dictionary<Type, string> _capabilityMap;

        public CapabilityPostConfigureOptions(IEnumerable<IMonitorCapability> capabilities)
        {
            _capabilities = capabilities.ToList();
            _capabilityMap = new Dictionary<Type, string>
            {
                { typeof(CallStacksOptions), MonitorCapability.CallStacks },
                { typeof(ExceptionsOptions), MonitorCapability.Exceptions },
                { typeof(ParameterCapturingOptions), MonitorCapability.ParameterCapturing },
                { typeof(MetricsOptions), MonitorCapability.Metrics }
            };
        }

        public void PostConfigure<TOptions>(bool isEnabled) where TOptions : class
        {
            if (_capabilityMap.TryGetValue(typeof(TOptions), out string? capabilityName))
            {
                IMonitorCapability? capability = _capabilities.FirstOrDefault(c => c.Name == capabilityName);
                if (capability != null)
                {
                    capability.Enabled = isEnabled;
                }
            }
        }

        public void PostConfigure(string? _, CallStacksOptions options) => PostConfigure<CallStacksOptions>(options.Enabled ?? false);
        public void PostConfigure(string? _, ExceptionsOptions options) => PostConfigure<ExceptionsOptions>(options.Enabled ?? false);
        public void PostConfigure(string? _, ParameterCapturingOptions options) => PostConfigure<ParameterCapturingOptions>(options.Enabled ?? false);
        public void PostConfigure(string? _, MetricsOptions options) => PostConfigure<MetricsOptions>(options.Enabled ?? false);
    }
}
