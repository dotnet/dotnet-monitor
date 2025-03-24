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
        [MinLength(1)]
        public string Name { get; set; }

        [JsonPropertyName("enabled")]
        public bool Enabled { get; }

        public MonitorCapability(string name, bool enabled)
        {
            Name = name;
            Enabled = enabled;
        }
    }
}
