// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

#if STARTUPHOOK
namespace Microsoft.Diagnostics.Monitoring.StartupHook.MonitorMessageDispatcher.Models
#else
namespace Microsoft.Diagnostics.Monitoring.Options
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
}
