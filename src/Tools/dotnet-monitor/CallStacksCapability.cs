// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Options;
using Microsoft.Extensions.Options;

namespace Microsoft.Diagnostics.Monitoring
{
    public class CallStacksCapability : IMonitorCapability
    {
        public string Name => MonitorCapabilityConstants.CallStacks;

        public bool Enabled { get; init; }

        public CallStacksCapability(
            IOptions<CallStacksOptions> options)
        {
            Enabled = options.Value.GetEnabled();
        }
    }
}
