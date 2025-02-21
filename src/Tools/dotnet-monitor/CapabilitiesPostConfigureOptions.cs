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
    public class CapabilityPostConfigureOptions : IPostConfigureOptions<CallStacksOptions>, IPostConfigureOptions<ExceptionsOptions>, IPostConfigureOptions<ParameterCapturingOptions>, IPostConfigureOptions<MetricsOptions>
    {
        private readonly List<IMonitorCapability> _capabilities;

        public CapabilityPostConfigureOptions(IEnumerable<IMonitorCapability> capabilities)
        {
            _capabilities = capabilities.ToList();
        }

        public void PostConfigure(string? name, CallStacksOptions options)
        {
            var capability = _capabilities.FirstOrDefault(c => c.Name == MonitorCapabilityName.CallStacks);
            if (capability != null)
            {
                capability.Enabled = options.Enabled == true;
            }
        }

        public void PostConfigure(string? name, ExceptionsOptions options)
        {
            var capability = _capabilities.FirstOrDefault(c => c.Name == MonitorCapabilityName.Exceptions);
            if (capability != null)
            {
                capability.Enabled = options.Enabled == true;
            }
        }

        public void PostConfigure(string? name, ParameterCapturingOptions options)
        {
            var capability = _capabilities.FirstOrDefault(c => c.Name == MonitorCapabilityName.ParameterCapturing);
            if (capability != null)
            {
                capability.Enabled = options.Enabled == true;
            }
        }

        public void PostConfigure(string? name, MetricsOptions options)
        {
            Console.WriteLine("Hit PostConfigure for Metrics");

            var capability = _capabilities.FirstOrDefault(c => c.Name == MonitorCapabilityName.Metrics);
            if (capability != null)
            {
                capability.Enabled = options.Enabled == true;
            }
        }
    }
}
