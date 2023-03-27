// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Monitoring.AzureMonitorDiagnostics;

internal sealed partial class AzureMonitorDiagnosticsEgressProviderOptions
{
    /// <summary>
    /// The connection string identifying the egress endpoint and instrumentation key.
    /// This is usually obtained from an Application Insights resource.
    /// </summary>
    [Required]
    public string ConnectionString { get; set; } = null!;

    /// <summary>
    /// Compression to apply during upload. May be "gzip" or "none".
    /// </summary>
    public CompressionType Compression { get; set; } = CompressionType.GZip;
}
