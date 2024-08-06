// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Monitoring.Options
{
    /// <summary>
    /// Configuration options for debugging dotnet-monitor features.
    /// These options are not officially supported and are subject to change.
    /// </summary>
    internal sealed class DotnetMonitorDebugOptions
    {
        public ExceptionsDebugOptions? Exceptions { get; set; }
    }
}
