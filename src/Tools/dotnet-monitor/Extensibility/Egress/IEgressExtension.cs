// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Tools.Monitor.Egress;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.Extensibility.Egress
{
    /// <summary>
    /// Interface used for calling an Egress Extension
    /// </summary>
    internal interface IEgressExtension : IExtension
    {
        /// <summary>
        /// Calls the given external extension to egress an artifact.
        /// </summary>
        /// <param name="configPayload">This will become the json configuration payload that starts the standard in stream.</param>
        /// <param name="getStreamAction">This <see cref="Func{Stream, CancellationToken, Task}" /> is used to get the artifact stream. The output stream is passed to this Func and the egress payload will be written to the provided stream.</param>
        /// <param name="token"><see cref="CancellationToken"/> for aborting egress.</param>
        /// <returns><see cref="EgressArtifactResult"/> representing the result of the operation, <see cref="EgressArtifactResult.Succeeded"/> should be checked.</returns>
        Task<EgressArtifactResult> EgressArtifact(ExtensionEgressPayload configPayload, Func<Stream, CancellationToken, Task> getStreamAction, CancellationToken token);
    }
}
