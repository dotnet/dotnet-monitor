// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    /// <summary>
    /// Base class for implementing a long running <see cref="IDiagnosticLifetimeService"/>.
    /// </summary>
    /// <remarks>
    /// Similar to <see cref="Microsoft.Extensions.Hosting.BackgroundService"/>.
    /// </remarks>
    internal abstract class DiagnosticLifetimeBackgroundService :
        IDiagnosticLifetimeService,
        IAsyncDisposable
    {
        private Task<Task>? _executingTask;
        private object _executionLock = new object();
        private CancellationTokenSource? _stoppingSource;

        public virtual ValueTask DisposeAsync()
        {
            _stoppingSource?.SafeCancel();

            return ValueTask.CompletedTask;
        }

        public async ValueTask StartAsync(CancellationToken cancellationToken)
        {
            if (null == _executingTask)
            {
                // Protect from concurrent calls of StartAsync as well as
                // race between calls of StartAsync and StopAsync.
                lock (_executionLock)
                {
                    if (null == _executingTask)
                    {
                        _stoppingSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                        // Begin to execute but don't wait for the service to complete.
                        _executingTask = ExecuteAsync(_stoppingSource.Token);
                    }
                }
            }

            // Wait for the service to start
            Task runningTask = await _executingTask;

            // If the service already completed (e.g. faulted, cancelled, etc),
            // await it to propagate the likely faulting or cancellation exception.
            if (runningTask.IsCompleted)
            {
                await runningTask;
            }
        }

        public async ValueTask StopAsync(CancellationToken cancellationToken)
        {
            if (null == _executingTask)
            {
                // Protect from concurrent calls of StopAsync as well as
                // race between calls of StartAsync and StopAsync.
                lock (_executionLock)
                {
                    // If execution never started, then there is no work to do.
                    if (null == _executingTask)
                    {
                        return;
                    }
                }
            }

            // Signal to the execution that it should stop.
            _stoppingSource?.SafeCancel();

            // Safe await the execution regardless of the completion type,
            // but allow cancelling waiting for it to finish.
            await _executingTask.Unwrap().SafeAwait().WaitAsync(cancellationToken);
        }

        /// <summary>
        /// Starts the service and returns a <see cref="Task"/> that completes when the service
        /// finishes running to completion.
        /// </summary>
        /// <param name="stoppingToken">The token to monitor for cancellation requests.</param>
        /// <returns>
        /// A task that completes when the service has started.
        /// The inner task completes when the service runs to completion.
        /// </returns>
        /// <remarks>
        /// The <paramref name="stoppingToken"/> applies to both the returned inner and outter task.
        /// </remarks>
        protected abstract Task<Task> ExecuteAsync(CancellationToken stoppingToken);
    }
}
