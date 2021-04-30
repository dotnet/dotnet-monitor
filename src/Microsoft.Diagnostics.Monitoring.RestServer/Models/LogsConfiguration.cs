// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Microsoft.Diagnostics.Monitoring.RestServer.Models
{
    public class LogsConfiguration
    {
        [JsonPropertyName("logLevel")]
        [EnumDataType(typeof(LogLevel))]
        [Required]
        public LogLevel LogLevel { get; set; } = LogLevel.Warning;

        [JsonPropertyName("filterSpecs")]
        public Dictionary<string, LogLevel?> FilterSpecs { get; set; }

        [JsonPropertyName("useAppFilters")]
        public bool UseAppFilters { get; set; } = true;
    }
}
