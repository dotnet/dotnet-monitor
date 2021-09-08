// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

#if UNITTEST
namespace Microsoft.Diagnostics.Monitoring.TestCommon
#else
namespace Microsoft.Diagnostics.Tools.Monitor
#endif
{
    internal static class TaskExtensions
    {
        public static async Task WithCancellation(this Task task, CancellationToken token)
        {
            using CancellationTokenSource localTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);

            try
            {
                // Creating a Task.Delay with Infinite timeout is not intended to work with a cancellation token that never fires
                // So we need to make a local "combined" token that we can cancel if the provided CancellationToken token is never canceled
                // This allows the framework to properly cleanup the Task and it's associated token registrations
                Task waitOnCancellation = Task.Delay(Timeout.Infinite, localTokenSource.Token);
                await Task.WhenAny(task, waitOnCancellation).Unwrap().ConfigureAwait(false);
            }
            finally
            {
                // Cancel to make sure Task.Delay token registration is removed.
                localTokenSource.Cancel();
            }
        }

        public static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken token)
        {
            await WithCancellation((Task)task, token);

            return task.Result;
        }
    }
}
