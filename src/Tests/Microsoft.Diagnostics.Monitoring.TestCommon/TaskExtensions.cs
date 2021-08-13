// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Microsoft.Diagnostics.Monitoring.TestCommon
{
    public static class TaskExtensions
    {
        public static async Task SafeAwait(this Task task, ITestOutputHelper outputHelper = null)
        {
            if (task != null)
            {
                try
                {
                    await task.ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    outputHelper?.WriteLine("Warning: Exception thrown while awaiting task: {0}", ex);
                }
            }
        }

        public static async Task<T> SafeAwait<T>(this Task<T> task, ITestOutputHelper outputHelper = null, T fallbackValue = default(T))
        {
            if (task != null)
            {
                try
                {
                    return await task.ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    outputHelper?.WriteLine("Warning: Exception thrown while awaiting task: {0}", ex);
                }
            }
            return fallbackValue;
        }

        public static async Task WithCancellation(this Task task, CancellationToken token)
        {
            CancellationTokenSource localCancellationTokenSource = new();
            CancellationTokenSource combinedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token, localCancellationTokenSource.Token);

            try
            {
                // Creating a Task.Delay with Infinite timeout is not intended to work with a cancellation token that never fires
                // So we need to make a local "combined" token that we can cancel if the provided CancellationToken token is never canceled
                // This allows the framework to properly cleanup the Task and it's associated token registrations
                Task waitOnCancellation = Task.Delay(Timeout.Infinite, combinedTokenSource.Token);
                await Task.WhenAny(task, waitOnCancellation).ConfigureAwait(false);
            }
            finally
            {
                // If the token provided wasn't cancelled, cancel our own token
                if (!token.IsCancellationRequested)
                {
                    localCancellationTokenSource.Cancel();
                }
            }
        }
    }
}
