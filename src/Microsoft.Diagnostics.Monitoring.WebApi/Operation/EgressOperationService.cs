// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal sealed class EgressOperationService : BackgroundService
    {
        private EgressOperationQueue _queue;
        private IServiceProvider _serviceProvider;
        private EgressOperationStore _operationsStore;

        public EgressOperationService(EgressOperationQueue taskQueue,
            IServiceProvider serviceProvider,
            EgressOperationStore operationStore)
        {
            _queue = taskQueue;
            _serviceProvider = serviceProvider;
            _operationsStore = operationStore;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                EgressRequest egressRequest = await _queue.DequeueAsync(stoppingToken);

                //Note we do not await these tasks, but we do limit how many can be executed at the same time
                _ = Task.Run( async ()=>
                {
                    await ExecuteEgressOperation(egressRequest, stoppingToken);
                }, stoppingToken);
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

                var result = await egressRequest.EgressOperation.ExecuteAsync(_serviceProvider, token);

                //It is possible that this operation never completes, due to infinite duration operations.

                _operationsStore.CompleteOperation(egressRequest.OperationId, result);
            }
        }
    }
}
