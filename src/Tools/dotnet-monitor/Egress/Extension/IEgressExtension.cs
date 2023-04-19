// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Tools.Monitor.Extensibility;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress
{
    /// <summary>
    /// Interface used for calling an Egress Extension
    /// </summary>
    internal interface IEgressExtension : IExtension
    {
        /// <summary>
        /// Calls the given external extension to egress an artifact.
        /// </summary>
        /// <param name="providerName">The egress provider name.</param>
        /// <param name="settings">The settings and context for the artifact to be egressed.</param>
        /// <param name="action">This <see cref="Func{Stream, CancellationToken, Task}" /> is used to get the artifact stream. The output stream is passed to this Func and the egress payload will be written to the provided stream.</param>
        /// <param name="token"><see cref="CancellationToken"/> for aborting egress.</param>
        /// <returns><see cref="EgressArtifactResult"/> representing the result of the operation, <see cref="EgressArtifactResult.Succeeded"/> should be checked.</returns>
        Task<EgressArtifactResult> EgressArtifact(
            string providerName,
            EgressArtifactSettings settings,
            Func<Stream, CancellationToken, Task> action,
            CancellationToken token);

        Task<EgressArtifactResult> ValidateProviderAsync(
            string providerName,
            EgressArtifactSettings settings,
            CancellationToken token);
    }
}
