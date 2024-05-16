// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using System;
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
            TaskCompletionSource backgroundTaskCompletion = new();
            using MockBackgroundService service = new MockBackgroundService(backgroundTaskCompletion.Task);

            // Act
            service.Start();
            await service.BackgroundTaskStarted.Task;

            // Assert
            Assert.False(service.BackgroundTaskWasCancelled);

            service.Stop();
            backgroundTaskCompletion.SetResult();
            await service.BackgroundTaskEnded.Task;

            Assert.True(service.BackgroundTaskWasCancelled);
            Assert.Null(service.BackgroundTaskException);
        }

        [Fact]
        public async Task DisposeWaitsForTheBackgroundTask()
        {
            // Arrange
            object lockObj = new();
            int callOrderMarker = 1;
            TaskCompletionSource backgroundTaskCompletion = new();

            // If Dispose() completes first, callOrderMarker will be 10
            // Otherwise, callOrderMarker will be 20
            void OnDisposeCompleted()
            {
                lock (lockObj)
                {
                    callOrderMarker *= 10;
                }
            }

            async Task BackgroundWork()
            {
                await backgroundTaskCompletion.Task;
                lock (lockObj)
                {
                    callOrderMarker += 1;
                }
            }

            MockBackgroundService service = new MockBackgroundService(BackgroundWork(), OnDisposeCompleted);

            // Act
            service.Start();
            await service.BackgroundTaskStarted.Task;
            service.Stop();

            Task disposeTask = Task.Run(service.Dispose);

            await service.DisposeStarted.Task;
            await Task.Delay(100); // Ensure that Dispose is waiting for the background task to complete
            backgroundTaskCompletion.SetResult();

            await disposeTask;

            // Assert
            Assert.Null(service.BackgroundTaskException);
            Assert.Equal(20, callOrderMarker);
        }

        [Fact]
        public async Task BackgroundTaskExceptionIsCaptured()
        {
            // Arrange
            static async Task BackgroundWork()
            {
                await Task.Yield();
                throw new NotImplementedException();
            }
            MockBackgroundService service = new MockBackgroundService(BackgroundWork());

            // Act
            service.Start();
            await service.BackgroundTaskStarted.Task;

            service.Stop();
            service.Dispose();

            // Assert
            Assert.IsType<NotImplementedException>(service.BackgroundTaskException);
        }
    }
}
