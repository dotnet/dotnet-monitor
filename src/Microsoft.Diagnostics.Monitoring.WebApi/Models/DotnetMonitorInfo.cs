// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

#if UNITTEST
namespace Microsoft.Diagnostics.Monitoring.UnitTests.Models
#else
namespace Microsoft.Diagnostics.Monitoring.WebApi.Models
#endif
{
    public class DotnetMonitorInfo
    {
        /// <summary>
        /// The Dotnet-Monitor version 
        /// </summary>
        [JsonPropertyName("version")]
        public string Version { get; set; }

        /// <summary>
        /// The dotnet runtime version 
        /// </summary>
        [JsonPropertyName("runtimeVersion")]
        public string RuntimeVersion { get; set; }

        /// <summary>
        /// Indicates whether Dotnet-Monitor is in Client mode or Listen mode
        /// </summary>
        [JsonPropertyName("listeningMode")]
        public string ListeningMode { get; set; }
    }
}
