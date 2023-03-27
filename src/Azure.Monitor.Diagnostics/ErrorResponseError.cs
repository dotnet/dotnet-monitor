// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Azure.Monitor.Diagnostics;

/// <summary>
/// The error object of an <see cref="ErrorResponse"/>.
/// </summary>
internal sealed class ErrorResponseError
{
    /// <summary>
    /// One of a server-defined set of error codes.
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// The human-readable representation of the error.
    /// </summary>
    public string? Message { get; set; }
}
