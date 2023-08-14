// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal sealed class EgressOperationService : BackgroundService
    {
        private IEgressOperationQueue _queue;
        private IServiceProvider _serviceProvider;
        private EgressOperationStore _operationsStore;

        public EgressOperationService(IServiceProvider serviceProvider,
            EgressOperationStore operationStore)
        {
            _queue = serviceProvider.GetRequiredService<IEgressOperationQueue>();
            _serviceProvider = serviceProvider;
            _operationsStore = operationStore;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                EgressRequest egressRequest = await _queue.DequeueAsync(stoppingToken);

                //Note we do not await these tasks, but we do limit how many can be executed at the same time
                _ = Task.Run(() => ExecuteEgressOperation(egressRequest, stoppingToken), stoppingToken);
            }
        }

        private async Task ExecuteEgressOperation(EgressRequest egressRequest, CancellationToken stoppingToken)
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

                    await egressRequest.EgressOperation.Started.WithCancellation(token).ConfigureAwait(false);
                    _operationsStore.MarkOperationAsRunning(egressRequest.OperationId);

                    ExecutionResult<EgressResult> result = await executeTask.WithCancellation(token).ConfigureAwait(false);

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
