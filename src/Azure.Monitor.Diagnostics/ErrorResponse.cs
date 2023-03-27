// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Azure.Monitor.Diagnostics;

/// <summary>
/// A minimal error response object.
/// See https://github.com/Microsoft/api-guidelines/blob/master/Guidelines.md#7102-error-condition-responses
/// </summary>
internal sealed class ErrorResponse
{
    /// <summary>
    /// The error object.
    /// </summary>
    public ErrorResponseError? Error { get; set; }
}
