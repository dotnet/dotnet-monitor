// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Azure.Monitor.Diagnostics;

/// <summary>
/// An application profile. The content of the response from an
/// app profile request.
/// </summary>
public sealed class AppProfile
{
    /// <summary>
    /// The AppId of the requested application.
    /// </summary>
    public Guid AppId { get; set; }
}
