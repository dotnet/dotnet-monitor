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
        public async Task StartBackgroundTask()
        {
            // Arrange
            using MockBackgroundService service = new MockBackgroundService();

            // Act
            service.Start();

            // Assert
            await service.BackgroundTaskStarted.Task;
        }

        [Fact]
        public async Task StopTriggersCancellation()
        {
            // Arrange
            using MockBackgroundService service = new MockBackgroundService(async (CancellationToken stoppingToken) =>
            {
                await Task.Delay(Timeout.Infinite, stoppingToken);
            });

            // Act
            service.Start();
            await service.BackgroundTaskStarted.Task;
            service.Stop();

            // Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.ExecutingTask!);
        }

        [Fact]
        public async Task StopWaitsForTheBackgroundTask()
        {
            // Arrange
            object lockObj = new();
            bool stopCompleted = false;
            bool taskCompleted = false;
            TaskCompletionSource backgroundTaskCompletion = new();
            TaskCompletionSource beforeStopCompletion = new();

            MockBackgroundService service = new MockBackgroundService(async _ =>
            {
                await backgroundTaskCompletion.Task;
                lock (lockObj)
                {
                    Assert.False(stopCompleted, "Stop completed before the background task.");
                    taskCompleted = true;
                }
            });

            // Act
            service.Start();
            await service.BackgroundTaskStarted.Task;

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

            await beforeStopCompletion.Task;
            // Wait a bit to ensure Stop() is waiting for the background task to complete
            await Task.Delay(TimeSpan.FromMilliseconds(100));

            backgroundTaskCompletion.SetResult();

            await stopTask;

            // Assert
            Assert.False(service.ExecutingTask?.IsFaulted);
        }

        [Fact]
        public async Task BackgroundTaskExceptionIsCaptured()
        {
            // Arrange
            MockBackgroundService service = new MockBackgroundService(async _ =>
            {
                await Task.Yield();
                throw new NotImplementedException();
            });

            // Act
            service.Start();
            await service.BackgroundTaskStarted.Task;

            service.Stop();
            service.Dispose();

            // Assert
            await Assert.ThrowsAsync<NotImplementedException>(() => service.ExecutingTask!);
        }
    }
}
