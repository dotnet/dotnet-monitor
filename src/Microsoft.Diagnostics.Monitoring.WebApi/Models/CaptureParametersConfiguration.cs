// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

#if STARTUPHOOK
namespace Microsoft.Diagnostics.Monitoring.StartupHook.MonitorMessageDispatcher.Models
#else
namespace Microsoft.Diagnostics.Monitoring.WebApi.Models
#endif
{
    public class CaptureParametersConfiguration
    {
        [JsonPropertyName("methods")]
        [Required, MinLength(1)]
        public MethodDescription[] Methods { get; set; } = Array.Empty<MethodDescription>();

        [JsonPropertyName("useDebuggerDisplayAttribute")]
        public bool UseDebuggerDisplayAttribute { get; set; } = true;

        [JsonPropertyName("captureLimit")]
        [Range(1, int.MaxValue)]
        public int? CaptureLimit { get; set; }
    }
}
