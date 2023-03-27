// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Azure.Monitor.Diagnostics;

/// <summary>
/// The response from a call to
/// <see cref="DiagnosticsClient.GetUploadTokenAsync(string, ArtifactKind, Guid, System.Threading.CancellationToken)"/>.
/// </summary>
public class UploadToken
{
    /// <summary>
    /// The URI and SAS token for a blob in Azure Blob Storage
    /// where the artifact can be uploaded.
    /// </summary>
    public Uri BlobUri { get; set; } = null!;
}
