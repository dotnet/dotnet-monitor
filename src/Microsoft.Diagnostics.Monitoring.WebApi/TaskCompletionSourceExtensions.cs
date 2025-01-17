// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal static class TaskCompletionSourceExtensions
    {
        /// <summary>
        /// Creates a <see cref="Task"/> that completes when the <see cref="TaskCompletionSource{TResult}"/> completes
        /// or is cancelled when the <see cref="CancellationToken"/> is signaled.
        /// </summary>
        /// <remarks>
        /// Signaling the <see cref="CancellationToken"/> will cancel the <see cref="TaskCompletionSource{TResult}"/>.
        /// If cancellation of the <see cref="TaskCompletionSource{TResult}"/> is not intended,
        /// use <see cref="TaskExtensions.WithCancellation{T}(Task{T}, CancellationToken)"/>
        /// on the <see cref="TaskCompletionSource{TResult}.Task"/> property.
        /// </remarks>
        public static async Task<T> WithCancellation<T>(this TaskCompletionSource<T> source, CancellationToken token)
        {
            using (token.Register(() => source.TrySetCanceled(token)))
            {
                return await source.Task.ConfigureAwait(false);
            }
        }

        public static async Task WithCancellation(this TaskCompletionSource source, CancellationToken token)
        {
            using (token.Register(() => source.TrySetCanceled(token)))
            {
                await source.Task.ConfigureAwait(false);
            }
        }
    }
}
