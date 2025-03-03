// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Microsoft.Diagnostics.Monitoring.Options
{
    public class MonitorCapability
    {
        public const string Exceptions = "exceptions";
        public const string ParameterCapturing = "parameters";
        public const string CallStacks = "callstacks";
        public const string Metrics = "metrics";
        public const string HttpEgress = "http_egress";

        [Required]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }
    }
}
