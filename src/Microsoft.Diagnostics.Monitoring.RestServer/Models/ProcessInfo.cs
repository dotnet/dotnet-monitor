// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json.Serialization;

namespace Microsoft.Diagnostics.Monitoring.RestServer.Models
{
    public class ProcessInfo
    {
        [JsonPropertyName("pid")]
        public int Pid { get; set; }

        [JsonPropertyName("uid")]
        public Guid Uid { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; private set; }

        [JsonPropertyName("commandLine")]
        public string CommandLine { get; private set; }

        [JsonPropertyName("operatingSystem")]
        public string OperatingSystem { get; private set; }

        [JsonPropertyName("processArchitecture")]
        public string ProcessArchitecture { get; private set; }

        internal static ProcessInfo FromProcessInfo(IProcessInfo processInfo)
        {
            return new ProcessInfo()
            {
                CommandLine = processInfo.CommandLine,
                Name = processInfo.ProcessName,
                OperatingSystem = processInfo.OperatingSystem,
                ProcessArchitecture = processInfo.ProcessArchitecture,
                Pid = processInfo.EndpointInfo.ProcessId,
                Uid = processInfo.EndpointInfo.RuntimeInstanceCookie
            };
        }
    }
}