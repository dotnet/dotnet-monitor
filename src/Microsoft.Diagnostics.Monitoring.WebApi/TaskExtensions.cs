// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal static class TaskExtensions
    {
        /// <summary>
        /// Creates a <see cref="Task"/> that completes when the provided <see cref="Task"/> completes, regardless
        /// of the completion state (success, faulted, cancelled).
        /// </summary>
        public static Task SafeAwait(this Task task)
        {
            return task.ContinueWith(
                _ => { },
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

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
                localTokenSource.SafeCancel();
            }
        }

        public static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken token)
        {
            await WithCancellation((Task)task, token);

            return task.Result;
        }
    }
}
