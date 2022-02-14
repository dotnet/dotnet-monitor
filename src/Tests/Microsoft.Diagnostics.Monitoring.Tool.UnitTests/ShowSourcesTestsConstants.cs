// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor;
using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    internal class ShowSourcesTestsConstants
    {
        public const string SimplifiedDiagnosticPort = "SimplifiedDiagnosticPort";
        public const string FullDiagnosticPort = "FullDiagnosticPort";
        public const string DiagnosticPort = nameof(RootOptions.DiagnosticPort);
        public const string EndpointName = $"{nameof(RootOptions.DiagnosticPort)}:{nameof(DiagnosticPortOptions.EndpointName)}";
        public const string ConnectionMode = $"{nameof(RootOptions.DiagnosticPort)}:{nameof(DiagnosticPortOptions.ConnectionMode)}";

   
        public static readonly Dictionary<string, string> DefaultProcess_EnvironmentVariables = new(StringComparer.Ordinal)
        {
            { "DefaultProcess:Filters:0:Key", "ProcessID" },
            { "DefaultProcess:Filters:0:Value", "12345" }
        };

        public static readonly Dictionary<string, string> DiagnosticPort_EnvironmentVariables = new(StringComparer.Ordinal)
        {
            { ConnectionMode, nameof(DiagnosticPortConnectionMode.Listen) },
            { EndpointName, "\\\\.\\pipe\\dotnet-monitor-pipe" }
        };

        public static readonly Dictionary<string, string> GlobalCounter_EnvironmentVariables = new(StringComparer.Ordinal)
        {
            { "GlobalCounter:IntervalSeconds", "2" },
        };
    }
}
