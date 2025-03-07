// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.Diagnostics.Monitoring.Options
{
    public class MonitorCapability : IMonitorCapability
    {
        [JsonPropertyName("name")]
        public string Name { get; init; }

        [JsonPropertyName("enabled")]
        public bool Enabled { get; init; }

        public MonitorCapability(string name, bool enabled)
        {
            Name = name;
            Enabled = enabled;
        }
    }
}
