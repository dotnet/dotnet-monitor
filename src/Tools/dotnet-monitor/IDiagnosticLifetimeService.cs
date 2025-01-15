// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    /// <summary>
    /// Callbacks for the lifetime events related to the diagnostic connection.
    /// </summary>
    public interface IDiagnosticLifetimeService
    {
        /// <summary>
        /// Invoked just as the target process is discovered and is available for diagnostics.
        /// </summary>
        /// <remarks>
        /// This is called just before the ResumeRuntime command is invoked.
        /// </remarks>
        ValueTask StartAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Invoke just after the target process is no longer available for diagnostics.
        /// </summary>
        /// <remarks>
        /// The target process may have terminated or is no longer responding to diagnostic requests.
        /// </remarks>
        ValueTask StopAsync(CancellationToken cancellationToken);
    }
}
