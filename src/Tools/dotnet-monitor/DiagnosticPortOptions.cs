// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if UNITTEST
namespace Microsoft.Diagnostics.Monitoring.UnitTests.Options
#else
namespace Microsoft.Diagnostics.Tools.Monitor
#endif
{
    public class DiagnosticPortOptions
    {
        public DiagnosticPortConnectionMode? ConnectionMode { get; set; }

        public string EndpointName { get; set; }

        public int? MaxConnections { get; set; }
    }

    public enum DiagnosticPortConnectionMode
    {
        Connect,
        Listen
    }
}
