// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.StartupHook
{
    internal abstract class BackgroundService : IDisposable
    {
        private readonly CancellationTokenSource _cts = new();
        private long _disposedState;

        public Task? ExecutingTask { get; private set; }

        public void Start()
        {
            ExecutingTask = Task.Run(async () =>
            {
                await ExecuteAsync(_cts.Token).ConfigureAwait(false);
            }, _cts.Token);
        }

        public void Stop()
        {
            SafeCancel();

            try
            {
                ExecutingTask?.Wait(TimeSpan.FromSeconds(1));
            }
            catch
            {
                // ignore
            }
        }

        public virtual void Dispose()
        {
            if (!DisposableHelper.CanDispose(ref _disposedState))
                return;

            SafeCancel();
            _cts.Dispose();
        }

        private void SafeCancel()
        {
            try
            {
                _cts.Cancel();
            }
            catch (AggregateException)
            {
                //  Ignore all exceptions thrown by registered callbacks on the associated CancellationToken.
            }
        }

        protected abstract Task ExecuteAsync(CancellationToken stoppingToken);
    }
}
