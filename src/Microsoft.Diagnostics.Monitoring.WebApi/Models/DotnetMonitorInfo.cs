﻿// Licensed to the .NET Foundation under one or more agreements.
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
        [JsonPropertyName("Version")]
        public string Version { get; set; }

        [JsonPropertyName("RuntimeVersion")]
        public string RuntimeVersion { get; set; }

        [JsonPropertyName("ListeningMode")]
        public string ListeningMode { get; set; }
    }
}
