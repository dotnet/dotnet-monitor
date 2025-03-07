// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Options;

namespace Microsoft.Diagnostics.Monitoring
{
    public class HttpEgressCapability : IMonitorCapability
    {
        public string Name => MonitorCapabilityConstants.HttpEgress;

        public bool Enabled { get; init; }

        public HttpEgressCapability(
            bool isEnabled)
        {
            Enabled = isEnabled;
        }
    }
}
