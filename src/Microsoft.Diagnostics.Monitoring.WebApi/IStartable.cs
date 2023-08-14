// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    /// <summary>
    /// Represents an operation that can produce a diagnostic
    /// artifact to the provided output stream.
    /// </summary>
    internal interface IStartable
    {
        Task Started { get; }
    }
}
