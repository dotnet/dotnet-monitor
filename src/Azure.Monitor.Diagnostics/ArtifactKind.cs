// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Azure.Monitor.Diagnostics;

/// <summary>
/// The kind of artifact that is being uploaded.
/// </summary>
public enum ArtifactKind
{
    /// <summary>
    /// Performance profile (e.g. .etl.zip, .diagsession or .nettrace files)
    /// </summary>
    Profile,

    /// <summary>
    /// Memory dumps (.dmp)
    /// </summary>
    Dump,

    /// <summary>
    /// Symbol (.pdb)
    /// </summary>
    Symbol,
}
