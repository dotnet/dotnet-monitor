// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Monitoring.AzureMonitorDiagnostics;

/// <summary>
/// Type of compression to use when uploading artifacts.
/// </summary>
public enum CompressionType
{
    /// <summary>
    /// No compression is used.
    /// </summary>
    None,

    /// <summary>
    /// GZip compression is used.
    /// </summary>
    GZip
}
