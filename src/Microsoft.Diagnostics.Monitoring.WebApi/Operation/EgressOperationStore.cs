// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal sealed class EgressOperationStore
    {
        private sealed class EgressEntry
        {
            public ExecutionResult<EgressResult> ExecutionResult { get; set; }
            public Models.OperationState State { get; set;}

            public EgressRequest EgressRequest { get; set; }

            public DateTime CreatedDateTime { get; } = DateTime.UtcNow;

            public Guid OperationId { get; set; }
        }

        private readonly Dictionary<Guid, EgressEntry> _requests = new();
        private readonly EgressOperationQueue _taskQueue;
        private readonly RequestLimitTracker _requestLimits;

        public EgressOperationStore(EgressOperationQueue queue, RequestLimitTracker requestLimits)
        {
            _taskQueue = queue;
            _requestLimits = requestLimits;
        }

        public async Task<Guid> AddOperation(IEgressOperation egressOperation, string limitKey)
        {
            Guid operationId = Guid.NewGuid();

            IDisposable limitTracker = _requestLimits.Increment(limitKey, out bool allowOperation);
            //We increment the limit here, and decrement it once the operation is cancelled or completed.
            //We do this here so that we can provide immediate errors if the user queues up too many operations.

            if (!allowOperation)
            {
                limitTracker.Dispose();
                throw new TooManyRequestsException();
            }

            var request = new EgressRequest(operationId, egressOperation, limitTracker);
            lock (_requests)
            {
                //Add operation object to central table.
                _requests.Add(operationId,
                    new EgressEntry
                    {
                        State = Models.OperationState.Running,
                        EgressRequest = request,
                        OperationId = operationId
                    });
            }

            //Kick off work to attempt egress
            await _taskQueue.EnqueueAsync(request);

            return operationId;
        }

        public void CancelOperation(Guid operationId)
        {
            lock (_requests)
            {
                if (!_requests.TryGetValue(operationId, out EgressEntry entry))
                {
                    throw new InvalidOperationException("Operation not found");
                }

                if (entry.State != Models.OperationState.Running)
                {
                    throw new InvalidOperationException("Operation is not running");
                }

                entry.State = Models.OperationState.Cancelled;
                entry.EgressRequest.CancellationTokenSource.Cancel();
                entry.EgressRequest.Dispose();
            }
        }

        public void CompleteOperation(Guid operationId, ExecutionResult<EgressResult> result)
        {
            lock (_requests)
            {
                if (!_requests.TryGetValue(operationId, out EgressEntry entry))
                {
                    throw new InvalidOperationException("Operation not found");
                }
                if (entry.State != Models.OperationState.Running)
                {
                    throw new InvalidOperationException("Operation not running");
                }

                entry.ExecutionResult = result;
                entry.EgressRequest.Dispose();

                if (entry.ExecutionResult.ProblemDetails == null)
                {
                    entry.State = Models.OperationState.Succeeded;
                }
                else
                {
                    entry.State = Models.OperationState.Failed;
                }
            }
        }

        public IEnumerable<Models.OperationSummary> GetOperations()
        {
            lock (_requests)
            {
                return _requests.Select((kvp) => new Models.OperationSummary
                {
                    Id = kvp.Key,
                    CreatedDateTime = kvp.Value.CreatedDateTime,
                    Status = kvp.Value.State
                }).ToList();
            }
        }

        public Models.OperationStatus GetOperationStatus(Guid operationId)
        {
            lock (_requests)
            {
                if (!_requests.TryGetValue(operationId, out EgressEntry entry))
                {
                    throw new InvalidOperationException("Operation not found");
                }

                var status = new Models.OperationStatus()
                {
                    OperationId = entry.EgressRequest.OperationId,
                    Status = entry.State,
                    CreatedDateTime = entry.CreatedDateTime,
                };

                if (entry.State == Models.OperationState.Succeeded)
                {
                    status.ResourceLocation = entry.ExecutionResult.Result.Value;
                }
                else if (entry.State == Models.OperationState.Failed)
                {
                    status.Error = new Models.OperationError
                    {
                        Code = entry.ExecutionResult.ProblemDetails.Status?.ToString(CultureInfo.InvariantCulture),
                        Message = entry.ExecutionResult.ProblemDetails.Detail
                    };
                }

                return status;
            }
        }
    }
}
