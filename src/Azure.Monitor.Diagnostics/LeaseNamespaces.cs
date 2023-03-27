// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Azure.Monitor.Diagnostics;

/// <summary>
/// Namespaces for agents to be used when requesting concurrency leases.
/// </summary>
public static class LeaseNamespaces
{
    /// <summary>
    /// Lease namespace for profiler agents.
    /// </summary>
    public const string Profiler = "profiler";
}
