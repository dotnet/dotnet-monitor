// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.StartupHook
{
    internal sealed class MockBackgroundService : BackgroundService, IDisposable
    {
        private readonly Func<CancellationToken, Task> _backgroundFunc;

        public MockBackgroundService()
        {
            _backgroundFunc = _ => Task.CompletedTask;
        }

        public MockBackgroundService(Func<CancellationToken, Task> backgroundFunc)
        {
            _backgroundFunc = backgroundFunc;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            BackgroundTaskStarted.SetResult();

            await _backgroundFunc(stoppingToken);
        }

        public override void Dispose()
        {
            base.Dispose();
        }

        public TaskCompletionSource BackgroundTaskStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
