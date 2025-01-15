// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Models
{
    public class DotnetMonitorInfo
    {
        /// <summary>
        /// The dotnet monitor version.
        /// </summary>
        [JsonPropertyName("version")]
        public string? Version { get; set; }

        /// <summary>
        /// The dotnet runtime version.
        /// </summary>
        [JsonPropertyName("runtimeVersion")]
        public string? RuntimeVersion { get; set; }

        /// <summary>
        /// Indicates whether dotnet monitor is in 'connect' mode or 'listen' mode.
        /// </summary>
        [JsonPropertyName("diagnosticPortMode")]
        public DiagnosticPortConnectionMode DiagnosticPortMode { get; set; }

        /// <summary>
        /// The name of the named pipe or unix domain socket to use for connecting to the diagnostic server.
        /// </summary>
        [JsonPropertyName("diagnosticPortName")]
        public string? DiagnosticPortName { get; set; }
    }
}
