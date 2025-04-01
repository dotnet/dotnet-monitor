// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Options;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Microsoft.Diagnostics.Monitoring.WebApi.Models
{
    public class DotnetMonitorInfo
    {
        [Description("The dotnet monitor version.")]
        [JsonPropertyName("version")]
        public string? Version { get; set; }

        [Description("The dotnet runtime version.")]
        [JsonPropertyName("runtimeVersion")]
        public string? RuntimeVersion { get; set; }

        /// <summary>
        /// Indicates whether dotnet monitor is in 'connect' mode or 'listen' mode.
        /// </summary>
        [JsonPropertyName("diagnosticPortMode")]
        public DiagnosticPortConnectionMode DiagnosticPortMode { get; set; }

        [Description("The name of the named pipe or unix domain socket to use for connecting to the diagnostic server.")]
        [JsonPropertyName("diagnosticPortName")]
        public string? DiagnosticPortName { get; set; }

        [Description("The capabilities provided by dotnet-monitor.")]
        [JsonPropertyName("capabilities")]
        public required MonitorCapability[] Capabilities { get; set; }
    }
}
