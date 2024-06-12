// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;

#if STARTUPHOOK
namespace Microsoft.Diagnostics.Monitoring.StartupHook.MonitorMessageDispatcher.Models
#else
using Microsoft.Diagnostics.Monitoring.Options;
namespace Microsoft.Diagnostics.Monitoring.WebApi.Models
#endif
{
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
