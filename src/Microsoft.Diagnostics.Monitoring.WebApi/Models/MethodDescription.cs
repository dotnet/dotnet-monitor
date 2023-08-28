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
    public interface IMethodDescription
    {
        [JsonPropertyName("moduleName")]
        public string ModuleName { get; set; }

        [JsonPropertyName("typeName")]
        public string TypeName { get; set; }

        [JsonPropertyName("methodName")]
        public string MethodName { get; set; }
    }

    public class MethodDescription : IMethodDescription
    {
        [Required]
        public string ModuleName { get; set; } = string.Empty;

        [Required]
        public string TypeName { get; set; } = string.Empty;

        [Required]
        public string MethodName { get; set; } = string.Empty;

        public override string ToString() => FormattableString.Invariant($"{ModuleName}!{TypeName}.{MethodName}");
    }
}
