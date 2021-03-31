// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    public static class TaskCompletionSourceExtensions
    {
        public static async Task<T> GetAsync<T>(this TaskCompletionSource<T> source, CancellationToken token)
        {
            TaskCompletionSource<T> cancellationSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
            using var _ = token.Register(() => cancellationSource.TrySetCanceled(token));

            return await Task.WhenAny(
                source.Task,
                cancellationSource.Task).Unwrap().ConfigureAwait(false);
        }
    }
}
