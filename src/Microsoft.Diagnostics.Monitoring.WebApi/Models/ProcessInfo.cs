// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json.Serialization;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Models
{
    public class ProcessInfo
    {
        [JsonPropertyName("pid")]
        public int Pid { get; set; }

        [JsonPropertyName("uid")]
        public Guid Uid { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("commandLine")]
        public string? CommandLine { get; set; }

        [JsonPropertyName("operatingSystem")]
        public string? OperatingSystem { get; set; }

        [JsonPropertyName("processArchitecture")]
        public string? ProcessArchitecture { get; set; }
    }
}
