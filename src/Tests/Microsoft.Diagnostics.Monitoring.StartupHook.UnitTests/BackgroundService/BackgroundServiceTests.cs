// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.StartupHook
{
    [TargetFrameworkMonikerTrait(TargetFrameworkMonikerExtensions.CurrentTargetFrameworkMoniker)]
    public sealed class BackgroundServiceTests
    {
        [Fact]
        public void ConstructionWorks()
        {
            using BackgroundService _ = new MockBackgroundService();
        }

        [Fact]
        public async Task Start_RunsBackgroundTask()
        {
            // Arrange
            using CancellationTokenSource cts = new(CommonTestTimeouts.GeneralTimeout);
            using MockBackgroundService service = new MockBackgroundService();

            // Act
            service.Start();

            // Assert
            await service.BackgroundTaskStarted.Task.WaitAsync(cts.Token);
        }

        [Fact]
        public async Task Stop_TriggersCancellation()
        {
            // Arrange
            using CancellationTokenSource cts = new(CommonTestTimeouts.GeneralTimeout);
            using MockBackgroundService service = new MockBackgroundService(async (CancellationToken stoppingToken) =>
            {
                await Task.Delay(Timeout.Infinite, stoppingToken);
            });

            // Act
            service.Start();
            await service.BackgroundTaskStarted.Task.WaitAsync(cts.Token);
            service.Stop();

            // Assert
            Assert.NotNull(service.ExecutingTask);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.ExecutingTask);
        }

        [Fact(Skip = "Flaky, the background service only waits up to 1 second when stopping")]
        public async Task Stop_WaitsForTheBackgroundTask()
        {
            // Arrange
            using CancellationTokenSource cts = new(CommonTestTimeouts.GeneralTimeout);
            object lockObj = new();
            bool stopCompleted = false;
            bool taskCompleted = false;
            TaskCompletionSource backgroundTaskCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource beforeStopCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);

            MockBackgroundService service = new MockBackgroundService(async _ =>
            {
                await backgroundTaskCompletion.Task.WaitAsync(cts.Token);
                lock (lockObj)
                {
                    Assert.False(stopCompleted, "Stop completed before the background task.");
                    taskCompleted = true;
                }
            });

            // Act
            service.Start();
            await service.BackgroundTaskStarted.Task.WaitAsync(cts.Token);

            Task stopTask = Task.Run(async () =>
            {
                await Task.Yield();
                beforeStopCompletion.SetResult();
                service.Stop();

                lock (lockObj)
                {
                    Assert.True(taskCompleted, "Stop completed before the background task.");
                    stopCompleted = true;
                }
            });

            await beforeStopCompletion.Task.WaitAsync(cts.Token);
            // Wait a bit to ensure Stop() is waiting for the background task to complete
            await Task.Delay(TimeSpan.FromMilliseconds(100));

            backgroundTaskCompletion.SetResult();

            await stopTask.WaitAsync(cts.Token);

            // Assert
            Assert.NotNull(service.ExecutingTask);
            Assert.False(service.ExecutingTask.IsFaulted);
        }

        [Fact]
        public async Task WorkerThrows_TaskExceptionIsCaptured()
        {
            // Arrange
            using CancellationTokenSource cts = new(CommonTestTimeouts.GeneralTimeout);
            MockBackgroundService service = new MockBackgroundService(async _ =>
            {
                await Task.Yield();
                throw new NotImplementedException();
            });

            // Act
            service.Start();
            await service.BackgroundTaskStarted.Task.WaitAsync(cts.Token);

            service.Stop();
            service.Dispose();

            // Assert
            Assert.NotNull(service.ExecutingTask);
            await Assert.ThrowsAsync<NotImplementedException>(() => service.ExecutingTask);
        }
    }
}
