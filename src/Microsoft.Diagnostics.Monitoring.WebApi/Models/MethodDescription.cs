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
    public class MethodDescription
    {
        // CONSIDER: Standardize this with our stack frame representation
        [JsonPropertyName("moduleName")]
        [Required]
        public string ModuleName { get; set; } = string.Empty;

        [JsonPropertyName("className")]
        [Required]
        public string ClassName { get; set; } = string.Empty;

        [JsonPropertyName("methodName")]
        [Required]
        public string MethodName { get; set; } = string.Empty;

        public override string ToString() => FormattableString.Invariant($"{ModuleName}!{ClassName}.{MethodName}");
    }
}
