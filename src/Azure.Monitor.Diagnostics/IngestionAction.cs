// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Azure.Monitor.Diagnostics;

/// <summary>
/// Supported constants for the "action" parameter in an artifact upload request.
/// </summary>
internal static class IngestionAction
{
    /// <summary>
    /// Request an upload token for a new artifact.
    /// </summary>
    public const string GetToken = "gettoken";

    /// <summary>
    /// Finalize an uploaded artifact, committing it to the service.
    /// </summary>
    public const string Commit = "commit";
}
