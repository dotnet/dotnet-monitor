// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

#if STARTUPHOOK || HOSTINGSTARTUP
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
    }

    public class MethodDescription
    {
        [JsonPropertyName("moduleName")]
        [Required]
        public string ModuleName { get; set; } = string.Empty;

        [JsonPropertyName("typeName")]
        [Required]
        public string TypeName { get; set; } = string.Empty;

        [JsonPropertyName("methodName")]
        [Required]
        public string MethodName { get; set; } = string.Empty;

        public override string ToString() => FormattableString.Invariant($"{ModuleName}!{TypeName}.{MethodName}");
    }
}
