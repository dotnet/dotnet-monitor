﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    public static class DiagnosticPortOptionsExtensions
    {
        public static DiagnosticPortConnectionMode GetConnectionMode(this DiagnosticPortOptions options) =>
            options.ConnectionMode.GetValueOrDefault(DiagnosticPortOptionsDefaults.ConnectionMode);

        public static bool GetDeleteEndpointOnStartup(this DiagnosticPortOptions options)
            => options.DeleteEndpointOnStartup.GetValueOrDefault(DiagnosticPortOptionsDefaults.DeleteEndpointOnStartup);
    }
}
