// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Models
{
    public class Activity
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("idFormat")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ActivityIdFormat IdFormat { get; set; } = ActivityIdFormat.Unknown;
    }
}
