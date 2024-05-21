// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.WebApi.UnitTests.Operation
{
    public class EgressOperationServiceTests
    {
        private sealed class TestEgressOperation(Func<TaskCompletionSource, CancellationToken, Task<ExecutionResult<EgressResult>>> executeFunc) : IEgressOperation
        {
            public bool IsStoppable => false;

            public ISet<string> Tags => new HashSet<string>();

            public string EgressProviderName => "NA";

            public EgressProcessInfo ProcessInfo => new EgressProcessInfo("dotnet", processId: 1, Guid.NewGuid());

            public Task Started => _startedCompletionSource.Task;

            private readonly TaskCompletionSource _startedCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

            public Task<ExecutionResult<EgressResult>> ExecuteAsync(IServiceProvider serviceProvider, CancellationToken token)
            {
                return executeFunc(_startedCompletionSource, token);
            }

            public Task StopAsync(CancellationToken token)
            {
                throw new NotImplementedException();
            }

            public void Validate(IServiceProvider serviceProvider)
            {
            }
        }

        private static EgressRequest CreateEgressRequest(Func<TaskCompletionSource, CancellationToken, Task<ExecutionResult<EgressResult>>> operationExecuteFunc)
            => new EgressRequest(Guid.NewGuid(), new TestEgressOperation(operationExecuteFunc), Mock.Of<IDisposable>());

        private static EgressRequest CreateEgressRequest(ExecutionResult<EgressResult> result)
            => CreateEgressRequest((startCompletionSource, token) =>
            {
                startCompletionSource.SetResult();
                return Task.FromResult(result);
            });


        private static EgressOperationService CreateEgressOperationService(IEgressOperationStore operationStore)
            => new EgressOperationService(Mock.Of<IServiceProvider>(), Mock.Of<IEgressOperationQueue>(), operationStore);


        [Fact]
        public async Task ExecuteAsync_Successful_TransitionsState_ToRunning()
        {
            // Arrange
            Mock<IEgressOperationStore> mockStore = new();
            using EgressOperationService service = CreateEgressOperationService(mockStore.Object);
            using CancellationTokenSource cts = new CancellationTokenSource(CommonTestTimeouts.GeneralTimeout);

            EgressRequest request = CreateEgressRequest(ExecutionResult<EgressResult>.Empty());

            // Act
            await service.ExecuteEgressOperationAsync(request, cts.Token);


            // Assert
            mockStore.Verify(
                m => m.MarkOperationAsRunning(request.OperationId),
                Times.Once());
        }

        [Fact]
        public async Task ExecuteAsync_Successful_TransitionsState_ToCompleted()
        {
            // Arrange
            Mock<IEgressOperationStore> mockStore = new();
            using EgressOperationService service = CreateEgressOperationService(mockStore.Object);
            using CancellationTokenSource cts = new CancellationTokenSource(CommonTestTimeouts.GeneralTimeout);

            EgressRequest request = CreateEgressRequest(ExecutionResult<EgressResult>.Empty());

            // Act
            await service.ExecuteEgressOperationAsync(request, cts.Token);


            // Assert
            mockStore.Verify(
                m => m.CompleteOperation(request.OperationId, It.IsAny<ExecutionResult<EgressResult>>()),
                Times.Once());
        }

        [Fact]
        public async Task ExecuteAsync_CompletesWithoutStarting_TransitionsState_ToCompleted()
        {
            // Arrange
            Mock<IEgressOperationStore> mockStore = new();
            using EgressOperationService service = CreateEgressOperationService(mockStore.Object);
            using CancellationTokenSource cts = new CancellationTokenSource(CommonTestTimeouts.GeneralTimeout);

            EgressRequest request = CreateEgressRequest((startCompletionSource, token) =>
            {
                // Don't signal the start completion source
                return Task.FromResult(ExecutionResult<EgressResult>.Empty());
            });

            // Act
            await service.ExecuteEgressOperationAsync(request, cts.Token);


            // Assert
            mockStore.Verify(
                m => m.CompleteOperation(request.OperationId, It.IsAny<ExecutionResult<EgressResult>>()),
                Times.Once());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ExecuteAsync_Failure_TransitionsState_ToFaulted(bool isStarted)
        {
            // Arrange
            Exception testException = new InvalidOperationException("test");
            Mock<IEgressOperationStore> mockStore = new();
            EgressRequest request = CreateEgressRequest((startCompletionSource, token) =>
            {
                if (isStarted)
                {
                    _ = startCompletionSource.TrySetResult();
                }
                throw testException;
            });

            TaskCompletionSource<ExecutionResult<EgressResult>> egressResultCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

            _ = mockStore
                .Setup(m => m.CompleteOperation(request.OperationId, It.IsAny<ExecutionResult<EgressResult>>()))
                .Callback((Guid id, ExecutionResult<EgressResult> result) =>
                {
                    egressResultCompletionSource.SetResult(result);
                });

            using EgressOperationService service = CreateEgressOperationService(mockStore.Object);
            using CancellationTokenSource cts = new CancellationTokenSource(CommonTestTimeouts.GeneralTimeout);

            // Act
            await service.ExecuteEgressOperationAsync(request, cts.Token);


            // Assert
            Assert.True(egressResultCompletionSource.Task.IsCompleted);
            ExecutionResult<EgressResult> result = await egressResultCompletionSource.Task;

            Assert.Equal(testException, result.Exception);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ExecuteAsync_Cancelled_TransitionsState_ToCancelled(bool isStarted)
        {
            // Arrange
            Mock<IEgressOperationStore> mockStore = new();
            using EgressOperationService service = CreateEgressOperationService(mockStore.Object);
            using CancellationTokenSource cts = new CancellationTokenSource(CommonTestTimeouts.GeneralTimeout);

            EgressRequest request = CreateEgressRequest(async (startCompletionSource, token) =>
            {
                if (isStarted)
                {
                    _ = startCompletionSource.TrySetResult();
                }

                await cts.CancelAsync();
                token.ThrowIfCancellationRequested();

                throw new InvalidOperationException("Should never reach here");
            });

            // Act & Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.ExecuteEgressOperationAsync(request, cts.Token));

            mockStore.Verify(
                m => m.CancelOperation(request.OperationId),
                Times.Once());
        }
    }
}
