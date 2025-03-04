// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Diagnostics.Monitoring.WebApi;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Diagnostics.Monitoring
{
    public class MetricsCapabilityPostConfigureOptions : CapabilityPostConfigureOptions<MetricsOptions>
    {
        public MetricsCapabilityPostConfigureOptions(IEnumerable<MonitorCapability> capabilities)
            : base(capabilities.First(c => c.Name == MonitorCapabilityConstants.Metrics))
        {
        }

        public override void PostConfigure(string? _, MetricsOptions options)
        {
            PostConfigure(options.Enabled ?? false);
        }
    }
}
