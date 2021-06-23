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
        private EgressOperationStore _operationStore;

        public EgressOperationService(EgressOperationQueue taskQueue,
            IServiceProvider serviceProvider,
            EgressOperationStore operationStore)
        {
            _queue = taskQueue;
            _serviceProvider = serviceProvider;
            _operationStore = operationStore;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var item = await _queue.DequeueAsync(stoppingToken);

                //We have two stopping tokens, one per item that can be triggered via Delete
                //and if we are stopping the service
                using (CancellationTokenSource linkedTokenSource =
                    CancellationTokenSource.CreateLinkedTokenSource(item.CancellationTokenSource.Token, stoppingToken))
                {
                    CancellationToken token = linkedTokenSource.Token;
                    var result = await item.EgressOperation.ExecuteAsync(_serviceProvider, token);

                    _operationStore.CompleteOperation(item.OperationId, result);
                }
            }
        }
    }
}
