// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    /// <summary>
    /// Represents an operation that can produce a diagnostic
    /// artifact to the provided output stream.
    /// </summary>
    internal interface IArtifactOperation : IAsyncDisposable
    {
        /// <summary>
        /// Start to produce a diagnostic artifact to the output stream.
        /// </summary>
        /// <param name="outputStream">The stream to which the diagnostic artifact is written.</param>
        /// <param name="token">A token used to cancel the operation.</param>
        Task StartAsync(
            Stream outputStream,
            CancellationToken token);

        /// <summary>
        /// Stops the production of the diagnostic artifact.
        /// </summary>
        /// <param name="token">A token used to cancel the stopping of the operation.</param>
        Task StopAsync(CancellationToken token);

        /// <summary>
        /// Waits for the production of the diagnostic artiface to complete.
        /// </summary>
        /// <param name="token">A token used to cancel waiting for completion.</param>
        Task WaitForCompletionAsync(CancellationToken token);

        /// <summary>
        /// Generates a file name for the associated artifact type.
        /// </summary>
        string GenerateFileName();

        /// <summary>
        /// Reports the content type of the diagnostic artifact.
        /// </summary>
        string ContentType { get; }

        /// <summary>
        /// Reports if the production of the diagnostic artifact is able to be stopped.
        /// </summary>
        bool IsStoppable { get; }
    }
}
