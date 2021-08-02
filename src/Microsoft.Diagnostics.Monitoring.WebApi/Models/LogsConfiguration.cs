// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

#if UNITTEST
namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests.Models
#else
namespace Microsoft.Diagnostics.Monitoring.WebApi.Models
#endif
{
    public class LogsConfiguration
    {
        /// <summary>
        /// The default level at which logs are collected.
        /// </summary>
        [JsonPropertyName("logLevel")]
        [EnumDataType(typeof(LogLevel))]
        [Required]
        public LogLevel LogLevel { get; set; } = LogLevel.Warning;

        /// <summary>
        /// The logger categories and levels at which logs are collected. Setting the log level to null will have logs collected from the corresponding category at the level set in the LogLevel property.
        /// </summary>
        [JsonPropertyName("filterSpecs")]
        public Dictionary<string, LogLevel?> FilterSpecs { get; set; }

        /// <summary>
        /// Set to true to collect logs at the application-defined categories and levels.
        /// </summary>
        [JsonPropertyName("useAppFilters")]
        public bool UseAppFilters { get; set; } = true;
    }
}
