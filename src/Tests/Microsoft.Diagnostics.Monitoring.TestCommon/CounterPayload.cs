// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json.Serialization;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    internal class CounterPayload
    {
        [JsonPropertyName("provider")]
        public string Provider { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("value")]
        public double Value { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("tags")]
        public string Metadata { get; set; }

        [JsonPropertyName("meterTags")]
        public string MeterTags { get; set; }

        [JsonPropertyName("instrumentTags")]
        public string InstrumentTags { get; set; }
    }
}
