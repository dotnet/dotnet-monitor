// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Monitoring.Options
{
    public class MonitorCapability : IMonitorCapability
    {
        public MonitorCapabilityName Name { get; }

        public bool Enabled { get; set; }
        public MonitorCapability(MonitorCapabilityName name)
        {
            Name = name;
        }

        public MonitorCapability(MonitorCapabilityName name, bool enabled)
        {
            Name = name;
            Enabled = enabled;
        }
    }

    public interface IMonitorCapability
    {
        MonitorCapabilityName Name { get; }
        bool Enabled
        {
            get; set;
        }
    }

    public enum MonitorCapabilityName
    {
        Exceptions,
        ParameterCapturing,
        CallStacks,
        Metrics,
        Https
    }
}
