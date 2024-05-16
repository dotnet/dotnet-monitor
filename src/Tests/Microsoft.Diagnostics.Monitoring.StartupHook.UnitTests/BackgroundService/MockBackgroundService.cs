// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.StartupHook
{
    internal sealed class MockBackgroundService : BackgroundService, IDisposable
    {
        private readonly Task _backgroundTask;
        private readonly Action _postDisposeAction;

        public MockBackgroundService()
        {
            _backgroundTask = Task.CompletedTask;
            _postDisposeAction = () => { };
        }

        public MockBackgroundService(Task backgroundTaskInput)
        {
            _backgroundTask = backgroundTaskInput;
            _postDisposeAction = () => { };
        }

        public MockBackgroundService(Task backgroundTaskInput, Action postDisposeAction)
        {
            _backgroundTask = backgroundTaskInput;
            _postDisposeAction = postDisposeAction;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            BackgroundTaskStarted.SetResult();

            await _backgroundTask;

            if (stoppingToken.IsCancellationRequested)
            {
                BackgroundTaskWasCancelled = true;
            }

            BackgroundTaskEnded.SetResult();
        }

        public override void Dispose()
        {
            DisposeStarted.SetResult();

            base.Dispose();

            _postDisposeAction();
        }

        public TaskCompletionSource BackgroundTaskStarted { get; } = new();

        public TaskCompletionSource BackgroundTaskEnded { get; } = new();

        public TaskCompletionSource DisposeStarted { get; } = new();

        public bool BackgroundTaskWasCancelled { get; private set; }
    }
}
