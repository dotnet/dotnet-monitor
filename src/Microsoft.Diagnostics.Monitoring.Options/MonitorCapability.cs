// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Microsoft.Diagnostics.Monitoring.Options
{
    public class MonitorCapability : IMonitorCapability
    {
        [JsonPropertyName("name")]
        [Required]
        public string Name { get; }

        [JsonPropertyName("enabled")]
        public bool Enabled { get; }

        public MonitorCapability(string name, bool enabled)
        {
            Name = name;
            Enabled = enabled;
        }
    }
}
