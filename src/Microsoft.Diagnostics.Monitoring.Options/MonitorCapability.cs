// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Monitoring.Options
{
    public class MonitorCapability : IMonitorCapability
    {
        public const string Exceptions = "exceptions";
        public const string ParameterCapturing = "parameters";
        public const string CallStacks = "callstacks";
        public const string Metrics = "metrics";
        public const string HttpEgress = "http_egress";

        public string Name { get; }

        public bool Enabled { get; set; }
        public MonitorCapability(string name)
        {
            Name = name;
        }

        public MonitorCapability(string name, bool enabled)
        {
            Name = name;
            Enabled = enabled;
        }
    }

    public interface IMonitorCapability
    {
        string Name { get; }
        bool Enabled
        {
            get; set;
        }
    }
}
