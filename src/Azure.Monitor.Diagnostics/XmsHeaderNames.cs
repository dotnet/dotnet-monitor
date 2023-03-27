// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Azure.Monitor.Diagnostics;

/// <summary>
/// HTTP header names in the x-ms namespace.
/// </summary>
internal static class XmsHeaderNames
{
    /// <summary>
    /// The action for operating on leases. (acquire, renew or release)
    /// </summary>
    public const string LeaseAction = "x-ms-lease-action";

    /// <summary>
    /// The lease duration in seconds.
    /// </summary>
    public const string LeaseDuration = "x-ms-lease-duration";

    /// <summary>
    /// The lease ID (a GUID)
    /// </summary>
    public const string LeaseId = "x-ms-lease-id";
}
