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
        private Task _executingTask;
        private CancellationTokenSource _stoppingSource;

        public virtual ValueTask DisposeAsync()
        {
            _stoppingSource?.SafeCancel();

            return ValueTask.CompletedTask;
        }

        public async ValueTask StartAsync(CancellationToken cancellationToken)
        {
            _stoppingSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // Begin to execute but don't wait for it to complete.
            _executingTask = ExecuteAsync(_stoppingSource.Token);

            // If task already completed (e.g. faulted, cancelled, etc),
            // await it to propagate the likely faulting or cancellation exception.
            if (_executingTask.IsCompleted)
            {
                await _executingTask;
            }
        }

        public async ValueTask StopAsync(CancellationToken cancellationToken)
        {
            // If execution never started, then there is no work to do.
            if (null == _executingTask)
            {
                return;
            }

            // Signal to the execution that it should stop.
            _stoppingSource.SafeCancel();

            // Safe await the execution regardless of the completion type,
            // but allow cancelling waiting for it to finish.
            await _executingTask.SafeAwait().WaitAsync(cancellationToken);
        }

        protected abstract Task ExecuteAsync(CancellationToken stoppingToken);
    }
}
