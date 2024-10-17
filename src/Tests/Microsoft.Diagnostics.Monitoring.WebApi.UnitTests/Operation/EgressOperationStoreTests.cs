// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.WebApi.UnitTests.Operation
{
    public class EgressOperationStoreTests
    {
        private const string AllowOperationKey = "allowed";
        private const string DenyOperationKey = "denied";

        private static EgressOperationStore SetupStore()
        {
            bool allowOperations;

            Mock<IRequestLimitTracker> mockRequestLimitTracker = new();
            mockRequestLimitTracker.Setup(tracker => tracker.Increment(It.IsAny<string>(), out allowOperations))
                .Callback((string key, out bool allowOperations) =>
                {
                    allowOperations = key.Equals(AllowOperationKey, StringComparison.OrdinalIgnoreCase);
                })
                .Returns(Mock.Of<IDisposable>());

            Mock<IServiceProvider> mockServiceProvider = new();
            mockServiceProvider.Setup(provider => provider.GetService(typeof(IRequestLimitTracker)))
                .Returns(mockRequestLimitTracker.Object);

            mockServiceProvider.Setup(provider => provider.GetService(typeof(IEgressOperationQueue)))
                .Returns(Mock.Of<IEgressOperationQueue>());

            return new EgressOperationStore(mockServiceProvider.Object);
        }

        [Fact]
        public async Task AddOperation_RespectsRequestLimit()
        {
            // Arrange
            EgressOperationStore store = SetupStore();

            // Act & Assert
            _ = await store.AddOperation(Mock.Of<IEgressOperation>(), AllowOperationKey);
            await Assert.ThrowsAsync<TooManyRequestsException>(() => store.AddOperation(Mock.Of<IEgressOperation>(), DenyOperationKey));
        }

        [Fact]
        public async Task StopOperation_OnException_InvokesCallback()
        {
            // Arrange
            EgressOperationStore store = SetupStore();

            Mock<IEgressOperation> mockOperation = new();
            mockOperation.Setup(operation => operation.StopAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ArgumentException("Test exception"));
            mockOperation.SetupGet(operation => operation.IsStoppable).Returns(true);

            TaskCompletionSource<Exception> exceptionHit = new(TaskCreationOptions.RunContinuationsAsynchronously);

            Guid operationId = await store.AddOperation(mockOperation.Object, AllowOperationKey);

            // Act
            store.StopOperation(operationId, (ex) => exceptionHit.TrySetResult(ex));

            // Assert
            Exception hitException = await exceptionHit.Task.WaitAsync(CommonTestTimeouts.GeneralTimeout);
            Assert.NotNull(hitException);
            Assert.Equal(WebApi.Models.OperationState.Stopping, store.GetOperationStatus(operationId).Status);
        }

        [Fact(Skip = "Flaky")]
        public async Task CancelOperation_Supports_StoppingState()
        {
            // Arrange
            EgressOperationStore store = SetupStore();

            Mock<IEgressOperation> mockOperation = new();
            mockOperation.SetupGet(operation => operation.IsStoppable).Returns(true);
            TaskCompletionSource<object> stopCancelled = new(TaskCreationOptions.RunContinuationsAsynchronously);
            mockOperation.Setup(operation => operation.StopAsync(It.IsAny<CancellationToken>()))
                .Callback(async (CancellationToken token) =>
                {
                    try
                    {
                        await Task.Delay(Timeout.InfiniteTimeSpan, token);
                    }
                    catch (OperationCanceledException)
                    {
                        stopCancelled.SetResult(null);
                    }
                });

            Guid operationId = await store.AddOperation(mockOperation.Object, AllowOperationKey);
            store.StopOperation(operationId, (ex) => { });
            Assert.Equal(WebApi.Models.OperationState.Stopping, store.GetOperationStatus(operationId).Status);

            // Act
            store.CancelOperation(operationId);

            // Assert
            await stopCancelled.Task.WaitAsync(CommonTestTimeouts.GeneralTimeout);
            Assert.Equal(WebApi.Models.OperationState.Cancelled, store.GetOperationStatus(operationId).Status);
        }
    }
}
