// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal sealed class EgressOperationService : BackgroundService
    {
        private readonly IEgressOperationQueue _queue;
        private readonly IServiceProvider _serviceProvider;
        private readonly IEgressOperationStore _operationsStore;

        public EgressOperationService(IServiceProvider serviceProvider,
            IEgressOperationQueue operationQueue,
            IEgressOperationStore operationStore)
        {
            _queue = operationQueue;
            _serviceProvider = serviceProvider;
            _operationsStore = operationStore;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                EgressRequest egressRequest = await _queue.DequeueAsync(stoppingToken);

                //Note we do not await these tasks, but we do limit how many can be executed at the same time
                _ = Task.Run(() => ExecuteEgressOperationAsync(egressRequest, stoppingToken), stoppingToken);
            }
        }

        // Internal for testing.
        internal async Task ExecuteEgressOperationAsync(EgressRequest egressRequest, CancellationToken stoppingToken)
        {
            //We have two stopping tokens, one per item that can be triggered via Delete
            //and if we are stopping the service
            using (CancellationTokenSource linkedTokenSource =
                CancellationTokenSource.CreateLinkedTokenSource(egressRequest.CancellationTokenSource.Token, stoppingToken))
            {
                CancellationToken token = linkedTokenSource.Token;
                token.ThrowIfCancellationRequested();

                try
                {
                    Task<ExecutionResult<EgressResult>> executeTask = egressRequest.EgressOperation.ExecuteAsync(_serviceProvider, token);
                    Task startTask = egressRequest.EgressOperation.Started;

                    await Task.WhenAny(startTask, executeTask).Unwrap().WaitAsync(token).ConfigureAwait(false);
                    if (startTask.IsCompleted)
                    {
                        _operationsStore.MarkOperationAsRunning(egressRequest.OperationId);
                    }

                    ExecutionResult<EgressResult> result = await executeTask.WaitAsync(token).ConfigureAwait(false);

                    //It is possible that this operation never completes, due to infinite duration operations.
                    _operationsStore.CompleteOperation(egressRequest.OperationId, result);
                }
                catch (OperationCanceledException)
                {
                    try
                    {
                        // Mirror the state in the operations store incase the operation was cancelled via another means besides
                        // the operations API.
                        _operationsStore.CancelOperation(egressRequest.OperationId);
                    }
                    // Expected if the state already reflects the cancellation.
                    catch (InvalidOperationException)
                    {

                    }

                    throw;
                }
                // This is unexpected, but an unhandled exception should still fail the operation.
                catch (Exception e)
                {
                    _operationsStore.CompleteOperation(egressRequest.OperationId, ExecutionResult<EgressResult>.Failed(e));
                }
            }
        }
    }
}
