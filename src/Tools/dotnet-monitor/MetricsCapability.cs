// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Options;

namespace Microsoft.Diagnostics.Monitoring
{
    public class MetricsCapability : IMonitorCapability
    {
        public string Name => MonitorCapabilityConstants.Metrics;

        public bool Enabled { get; init; }

        public MetricsCapability(
            IOptions<MetricsOptions> options)
        {
            Enabled = options.Value.GetEnabled();
        }
    }
}
