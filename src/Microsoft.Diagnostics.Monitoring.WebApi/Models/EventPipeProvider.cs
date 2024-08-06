// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Imported by Microsoft.Diagnostics.Monitoring.ConfigurationSchema
#nullable enable

#if !SCHEMAGEN
using Microsoft.Diagnostics.Monitoring.WebApi.Validation;
#endif
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Tracing;
using System.Text.Json.Serialization;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Models
{
    public class EventPipeProvider
    {
        [JsonPropertyName("name")]
        [Required]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("keywords")]
#if !SCHEMAGEN
        [IntegerOrHexString]
#endif
        public string? Keywords { get; set; } = "0x" + EventKeywords.All.ToString("X");

        [JsonPropertyName("eventLevel")]
        [EnumDataType(typeof(EventLevel))]
        public EventLevel EventLevel { get; set; } = EventLevel.Verbose;

        [JsonPropertyName("arguments")]
        public IDictionary<string, string>? Arguments { get; set; }
    }
}
