// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    internal class DiagnosticPortTestsConstants
    {
        public const string SimplifiedDiagnosticPort = "SimplifiedDiagnosticPort";
        public const string FullDiagnosticPort = "FullDiagnosticPort";

        public static readonly Dictionary<string, string> SimplifiedListen_EnvironmentVariables = new(StringComparer.Ordinal)
        {
            { "DiagnosticPort", SimplifiedDiagnosticPort }
        };

        public static readonly Dictionary<string, string> FullListen_EnvironmentVariables = new(StringComparer.Ordinal)
        {
            { "DiagnosticPort:ConnectionMode", nameof(DiagnosticPortConnectionMode.Listen) },
            { "DiagnosticPort:EndpointName", FullDiagnosticPort }
        };

        public static readonly Dictionary<string, string> Connect_EnvironmentVariables = new(StringComparer.Ordinal)
        {
            { "DiagnosticPort:ConnectionMode", nameof(DiagnosticPortConnectionMode.Connect) }
        };

        public static readonly Dictionary<string, string> AllListen_EnvironmentVariables = new(StringComparer.Ordinal)
        {
            { "DiagnosticPort", SimplifiedDiagnosticPort },
            { "DiagnosticPort:ConnectionMode", nameof(DiagnosticPortConnectionMode.Listen) },
            { "DiagnosticPort:EndpointName", FullDiagnosticPort }
        };

        /*
                 public static readonly Dictionary<string, Dictionary<string, string>> DiagnosticPort_EnvironmentVariables = new(StringComparer.Ordinal)
        {
            {
                "FullListen", new(StringComparer.Ordinal) 
                {
                    { "DiagnosticPort:ConnectionMode", nameof(DiagnosticPortConnectionMode.Listen) },
                    { "DiagnosticPort:EndpointName", FullDiagnosticPort }
                }
            },
            {
                "SimplifiedListen",
                new(StringComparer.Ordinal)
                {
                    { "DiagnosticPort", SimplifiedDiagnosticPort }
                }
            },
            {
                "Connect",
                new(StringComparer.Ordinal)
                {
                    { "DiagnosticPort:ConnectionMode", nameof(DiagnosticPortConnectionMode.Connect) }
                }
            },
            {
                "AllListen",
                new(StringComparer.Ordinal)
                {
                    { "DiagnosticPort:ConnectionMode", nameof(DiagnosticPortConnectionMode.Listen) },
                    { "DiagnosticPort:EndpointName", FullDiagnosticPort },
                    { "DiagnosticPort", SimplifiedDiagnosticPort }
                }
            }
        };
         */

    }
}
