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
        [JsonPropertyName("assemblyName")]
        [Required]
        public string AssemblyName { get; set; } = string.Empty;

        [JsonPropertyName("typeName")]
        [Required]
        public string TypeName { get; set; } = string.Empty;

        [JsonPropertyName("methodName")]
        [Required]
        public string MethodName { get; set; } = string.Empty;

        public override string ToString() => FormattableString.Invariant($"{AssemblyName}!{TypeName}.{MethodName}");
    }
}
