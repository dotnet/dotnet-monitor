// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Extensions.Options;

namespace Microsoft.Diagnostics.Monitoring
{
    public class ParametersCapability : IMonitorCapability
    {
        public string Name => MonitorCapabilityConstants.ParameterCapturing;

        public bool Enabled { get; init; }

        public ParametersCapability(
            IOptions<ParameterCapturingOptions> options)
        {
            Enabled = options.Value.GetEnabled();
        }
    }
}
