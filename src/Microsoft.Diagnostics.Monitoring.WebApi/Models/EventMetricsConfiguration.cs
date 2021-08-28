// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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
