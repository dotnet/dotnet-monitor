// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor;
using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    internal static class DiagnosticPortTestsConstants
    {
        public const string SimplifiedDiagnosticPort = "SimplifiedDiagnosticPort";
        public const string FullDiagnosticPort = "FullDiagnosticPort";
        public const string DiagnosticPort = nameof(RootOptions.DiagnosticPort);
        public const string EndpointName = $"{nameof(RootOptions.DiagnosticPort)}:{nameof(DiagnosticPortOptions.EndpointName)}";
        public const string ConnectionMode = $"{nameof(RootOptions.DiagnosticPort)}:{nameof(DiagnosticPortOptions.ConnectionMode)}";

        public static readonly Dictionary<string, string> SimplifiedListen_EnvironmentVariables = new(StringComparer.Ordinal)
        {
            { DiagnosticPort, SimplifiedDiagnosticPort }
        };

        public static readonly Dictionary<string, string> FullListen_EnvironmentVariables = new(StringComparer.Ordinal)
        {
            { ConnectionMode, nameof(DiagnosticPortConnectionMode.Listen) },
            { EndpointName, FullDiagnosticPort }
        };

        public static readonly Dictionary<string, string> Connect_EnvironmentVariables = new(StringComparer.Ordinal)
        {
            { ConnectionMode, nameof(DiagnosticPortConnectionMode.Connect) }
        };

        public static readonly Dictionary<string, string> AllListen_EnvironmentVariables = new(StringComparer.Ordinal)
        {
            { DiagnosticPort, SimplifiedDiagnosticPort },
            { ConnectionMode, nameof(DiagnosticPortConnectionMode.Listen) },
            { EndpointName, FullDiagnosticPort }
        };
    }
}
