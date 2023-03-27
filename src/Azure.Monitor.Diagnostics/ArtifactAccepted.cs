// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Azure.Monitor.Diagnostics;

/// <summary>
/// Response from successfully committing an artifact.
/// </summary>
public sealed class ArtifactAccepted
{
    /// <summary>
    /// The server timestamp when the blob was accepted.
    /// </summary>
    public DateTime AcceptedTime { get; set; }

    /// <summary>
    /// URI for the accepted blob.
    /// </summary>
    public Uri BlobUri { get; set; } = null!;

    /// <summary>
    /// The server-side telemetry ID of the request that accepted the artifact.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// The stamp ID. May be a customer managed storage stamp ID.
    /// </summary>
    public string StampId { get; set; } = null!;

    /// <summary>
    /// The artifact location ID. This is an opaque ID that clients
    /// should use to retrieve the accepted artifact. When connected
    /// to Application Insights, for example, the client typically
    /// sends this ID to Application Insights in an indexing event.
    /// </summary>
    public string ArtifactLocationId { get; set; } = null!;
}
