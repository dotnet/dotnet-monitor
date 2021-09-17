// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        public string Name { get; set; }

        [JsonPropertyName("keywords")]
#if !SCHEMAGEN
        [IntegerOrHexString]
#endif
        public string Keywords { get; set; } = "0x" + EventKeywords.All.ToString("X");

        [JsonPropertyName("eventLevel")]
        [EnumDataType(typeof(EventLevel))]
        public EventLevel EventLevel { get; set; } = EventLevel.Verbose;

        [JsonPropertyName("arguments")]
        public IDictionary<string, string> Arguments { get; set; }
    }
}
