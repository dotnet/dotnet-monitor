// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

#if UNITTEST
namespace Microsoft.Diagnostics.Monitoring.UnitTests.Options
#else
namespace Microsoft.Diagnostics.Tools.Monitor
#endif
{
    public class DiagnosticPortOptions
    {
        [Description(@"In Connect mode, dotnet-monitor connects to the application for diagnostics. In Listen
mode the application connects to dotnet-monitor via " + nameof(EndpointName))]
        [DefaultValue(DiagnosticPortOptionsDefaults.ConnectionMode)]
        public DiagnosticPortConnectionMode? ConnectionMode { get; set; }

        [Description(@"In Listen mode, specifies the name of the named pipe or unix domain socket to use for connecting
to the diagnostic server")]
        public string EndpointName { get; set; }

        [Description(@"In Listen mode, the maximum amount of connections to accept.")]
        public int? MaxConnections { get; set; }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DiagnosticPortConnectionMode
    {
        Connect,
        Listen
    }
}
