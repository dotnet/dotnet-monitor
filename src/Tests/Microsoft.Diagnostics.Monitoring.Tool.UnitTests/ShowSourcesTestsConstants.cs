// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor;
using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    internal static class ShowSourcesTestsConstants
    {
        // The values used here correspond to the ones used in SampleConfigurations

        public const string DiagnosticPort = nameof(RootOptions.DiagnosticPort);
        public const string EndpointName = $"{nameof(RootOptions.DiagnosticPort)}:{nameof(DiagnosticPortOptions.EndpointName)}";
        public const string ConnectionMode = $"{nameof(RootOptions.DiagnosticPort)}:{nameof(DiagnosticPortOptions.ConnectionMode)}";
        public const string IntervalSeconds = $"{nameof(RootOptions.GlobalCounter)}:{nameof(GlobalCounterOptions.IntervalSeconds)}";
        public const string FiltersKey = $"{nameof(RootOptions.DefaultProcess)}:{nameof(ProcessFilterOptions.Filters)}:0:{nameof(ProcessFilterDescriptor.Key)}";
        public const string FiltersValue = $"{nameof(RootOptions.DefaultProcess)}:{nameof(ProcessFilterOptions.Filters)}:0:{nameof(ProcessFilterDescriptor.Value)}";

        public const string IntervalSecondsValue = "2";
        public const string ProcessIdValue = "12345";
        public const string DiagnosticPortValue = "\\\\.\\pipe\\dotnet-monitor-pipe";

        public static readonly Dictionary<string, string> DefaultProcess_EnvironmentVariables = new(StringComparer.Ordinal)
        {
            { FiltersKey, nameof(ProcessKey.ProcessId) },
            { FiltersValue, ProcessIdValue }
        };

        public static readonly Dictionary<string, string> DiagnosticPort_EnvironmentVariables = new(StringComparer.Ordinal)
        {
            { ConnectionMode, nameof(DiagnosticPortConnectionMode.Listen) },
            { EndpointName, DiagnosticPortValue }
        };

        public static readonly Dictionary<string, string> GlobalCounter_EnvironmentVariables = new(StringComparer.Ordinal)
        {
            { IntervalSeconds, IntervalSecondsValue },
        };
    }
}
