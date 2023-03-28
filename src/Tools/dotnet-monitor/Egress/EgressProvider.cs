// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress
{
    /* 
     * == Egress Provider Design ==
     * - Each type of egress is implemented as an EgressProvider<TOptions>. The following are the built-in providers:
     *   - FileSystemEgressProvider: Allows egressing stream data to the file system.
     *   The egress provider options are typically use for describing to where stream data is to be egressed.
     * - When invoking an egress provider, the following are required:
     *   - an options instance describing to where the artifact should be egressed
     *   - an action for acquiring the stream data.
     *   - a settings instance describing aspects of the artifact, such as its file name and content type.
     *   The acquisition action can either provide the stream or allow the provider to provision the
     *   stream, which is passed into the action.
     * - When an egress provider finishes egressing stream data, it will return a value that identifies the location
     *   of where the stream data was egressed.
     */

    /// <summary>
    /// Base class for all egress implementations.
    /// </summary>
    /// <typeparam name="TOptions">Type of provider options class.</typeparam>
    /// <remarks>
    /// The <typeparamref name="TOptions"/> type is typically used for providing information
    /// about to where a stream is egressed (e.g. directory path, blob storage account, etc).
    /// Egress providers should throw <see cref="EgressException"/> when operational error occurs
    /// (e.g. unable to write out stream data). Nearly all other exceptions are treats as programming
    /// errors with the exception of <see cref="OperationCanceledException"/> and <see cref="ValidationException"/>.</remarks>
    internal abstract class EgressProvider<TOptions> :
        IEgressProvider<TOptions>
        where TOptions : IEgressProviderCommonOptions
    {
        protected EgressProvider(ILogger logger)
        {
            Logger = logger;
        }

        /// <summary>
        /// Egress a stream via a callback by writing to the provided stream.
        /// </summary>
        /// <param name="options">Described to where stream data should be egressed.</param>
        /// <param name="action">Callback that is invoked in order to write data to the provided stream.</param>
        /// <param name="artifactSettings">Describes data about the artifact, such as file name and content type.</param>
        /// <param name="token">The token to monitor for cancellation requests.</param>
        /// <returns>A task that completes with a value of the identifier of the egress result. Typically,
        /// this is a path to access the stream without any information indicating whether any particular
        /// user has access to it (e.g. no file system permissions or SAS tokens).</returns>
        public abstract Task<string> EgressAsync(
            string providerType,
            string providerName,
            TOptions options,
            Func<Stream, CancellationToken, Task> action,
            EgressArtifactSettings artifactSettings,
            CancellationToken token);

        protected ILogger Logger { get; }
    }
}
