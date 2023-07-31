// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    /// <summary>
    /// Represents an operation that can produce a diagnostic
    /// artifact inside the target process
    /// </summary>
    internal interface IInProcessOperation
    {
        /// <summary>
        /// Produces a diagnostic artifact inside the target process
        /// </summary>
        /// <param name="startCompletionSource">A completion source that is signaled when the operation has started.</param>
        /// <param name="token">A token used to cancel the operation.</param>
        /// <returns></returns>
        Task ExecuteAsync(
            TaskCompletionSource<object> startCompletionSource,
            CancellationToken token);

        /// <summary>
        /// Stops the production of the diagnostic artifact.
        /// </summary>
        /// <param name="token">A token used to cancel the stopping of the operation.</param>
        Task StopAsync(CancellationToken token);

        /// <summary>
        /// Reports if the production of the diagnostic artifact is able to be stopped.
        /// </summary>
        bool IsStoppable { get; }
    }
}
