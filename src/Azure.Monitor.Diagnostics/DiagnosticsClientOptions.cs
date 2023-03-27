// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Core;
using System;

namespace Azure.Monitor.Diagnostics;

/// <summary>
/// Options for the diagnostics client.
/// </summary>
public class DiagnosticsClientOptions
{
    /// <summary>
    /// The endpoint of the ingestion service.
    /// Defaults to https://profiler.monitor.azure.com/
    /// </summary>
    public Uri Endpoint { get; set; } = new Uri("https://profiler.monitor.azure.com/");

    /// <summary>
    /// The api-version to use.
    /// Defaults to 2023-01-10
    /// </summary>
    public string ApiVersion { get; set; } = "2023-01-10";

    /// <summary>
    /// A TokenCredential to use for authentication.
    /// May be null if your Application Insights resource allows public
    /// ingestion.
    /// </summary>
    public TokenCredential? TokenCredential { get; set; }
}
