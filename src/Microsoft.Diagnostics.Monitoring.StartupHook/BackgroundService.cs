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
        private Task? _executeTask;
        private long _disposedState;

        public void Start()
        {
            _executeTask = Task.Run(async () =>
            {
                try
                {
                    await ExecuteAsync(_cts.Token);
                }
                catch (OperationCanceledException)
                {
                    // Ignore
                }
                catch (Exception ex)
                {
                    BackgroundTaskException = ex;
                }
            });
        }

        public void Stop()
        {
            _cts.Cancel();
        }

        public Exception? BackgroundTaskException { get; private set; }

        public virtual void Dispose()
        {
            if (!DisposableHelper.CanDispose(ref _disposedState))
                return;

            _executeTask?.Wait();
            _executeTask = null;

            _cts.Dispose();
        }

        protected abstract Task ExecuteAsync(CancellationToken stoppingToken);
    }
}
