﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Models
{
    public class EventMetricsConfiguration

    {
        [JsonPropertyName("includeDefaultProviders")]
        public bool IncludeDefaultProviders { get; set; } = true;

        [JsonPropertyName("providers")]
        public EventMetricsProvider[] Providers { get; set; }
    }

    public class EventMetricsProvider
    {
        [Required]
        [JsonPropertyName("providerName")]
        public string ProviderName { get; set; }

        [JsonPropertyName("counterNames")]
        public string[] CounterNames { get; set; }
    }
}
