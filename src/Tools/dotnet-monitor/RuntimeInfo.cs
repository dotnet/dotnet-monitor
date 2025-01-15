// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    public static class RuntimeInfo
    {
        public static bool IsDiagnosticsEnabled
        {
            get
            {
                string? enableDiagnostics = Environment.GetEnvironmentVariable("DOTNET_EnableDiagnostics") ?? Environment.GetEnvironmentVariable("COMPlus_EnableDiagnostics");
                return string.IsNullOrEmpty(enableDiagnostics) || !"0".Equals(enableDiagnostics, StringComparison.Ordinal);
            }
        }

        public static bool IsInKubernetes => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("KUBERNETES_SERVICE_HOST"));
    }
}
