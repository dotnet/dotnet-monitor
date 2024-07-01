// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal sealed class EgressOperationStore : IEgressOperationStore
    {
        private sealed class EgressEntry
        {
            public bool IsStoppable
            {
                get
                {
                    return (State == Models.OperationState.Starting || State == Models.OperationState.Running) && EgressRequest.EgressOperation.IsStoppable;
                }
            }

#nullable disable
            public ExecutionResult<EgressResult> ExecutionResult { get; set; }
#nullable restore

            public required Models.OperationState State { get; set; }

            public required EgressRequest EgressRequest { get; set; }

            public DateTime CreatedDateTime { get; } = DateTime.UtcNow;

            public required Guid OperationId { get; set; }

            public required ISet<string> Tags { get; set; }

            public TaskCompletionSource TaskCompletionSource { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        private readonly Dictionary<Guid, EgressEntry> _requests = new();
        private readonly IEgressOperationQueue _taskQueue;
        private readonly IRequestLimitTracker _requestLimits;
        private readonly IServiceProvider _serviceProvider;

        public EgressOperationStore(IServiceProvider serviceProvider)
        {
            _taskQueue = serviceProvider.GetRequiredService<IEgressOperationQueue>();
            _requestLimits = serviceProvider.GetRequiredService<IRequestLimitTracker>();
            _serviceProvider = serviceProvider;
        }

        public async Task<ExecutionResult<EgressResult>> ExecuteOperation(IEgressOperation egressOperation)
        {
            // Collection Rules do not follow request limits.

            EgressEntry entry = await AddOperationInternal(egressOperation, RequestLimitTracker.Unlimited);

            await entry.TaskCompletionSource.Task;

            return entry.ExecutionResult;
        }

        public async Task<Guid> AddOperation(IEgressOperation egressOperation, string limitKey)
        {
            EgressEntry entry = await AddOperationInternal(egressOperation, limitKey);
            return entry.OperationId;
        }

        private async Task<EgressEntry> AddOperationInternal(IEgressOperation egressOperation, string limitKey)
        {
            egressOperation.Validate(_serviceProvider);

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
            var egressEntry = new EgressEntry
            {
                State = Models.OperationState.Starting,
                EgressRequest = request,
                OperationId = operationId,
                Tags = request.EgressOperation.Tags
            };

            lock (_requests)
            {
                //Add operation object to central table.
                _requests.Add(operationId, egressEntry);
            }
            await _taskQueue.EnqueueAsync(request);

            return egressEntry;
        }

        public void StopOperation(Guid operationId, Action<Exception> onStopException)
        {
            lock (_requests)
            {
                if (!_requests.TryGetValue(operationId, out EgressEntry? entry))
                {
                    throw new InvalidOperationException(Strings.ErrorMessage_OperationNotFound);
                }

                if (entry.State != Models.OperationState.Starting &&
                    entry.State != Models.OperationState.Running)
                {
                    throw new InvalidOperationException(Strings.ErrorMessage_OperationNotRunning);
                }

                if (!entry.EgressRequest.EgressOperation.IsStoppable)
                {
                    throw new InvalidOperationException(Strings.ErrorMessage_OperationDoesNotSupportBeingStopped);
                }

                entry.State = Models.OperationState.Stopping;

                CancellationToken token = entry.EgressRequest.CancellationTokenSource.Token;
                _ = Task.Run(() => entry.EgressRequest.EgressOperation.StopAsync(token), token)
                    .ContinueWith(task => onStopException(task.Exception!),
                    token,
                    TaskContinuationOptions.OnlyOnFaulted,
                    TaskScheduler.Default);
            }
        }

        public void MarkOperationAsRunning(Guid operationId)
        {
            lock (_requests)
            {
                if (!_requests.TryGetValue(operationId, out EgressEntry? entry))
                {
                    throw new InvalidOperationException(Strings.ErrorMessage_OperationNotFound);
                }

                if (entry.State != Models.OperationState.Starting)
                {
                    throw new InvalidOperationException(Strings.ErrorMessage_OperationNotStarting);
                }

                entry.State = Models.OperationState.Running;
            }
        }

        public void CancelOperation(Guid operationId)
        {
            lock (_requests)
            {
                if (!_requests.TryGetValue(operationId, out EgressEntry? entry))
                {
                    throw new InvalidOperationException(Strings.ErrorMessage_OperationNotFound);
                }

                if (entry.State != Models.OperationState.Starting &&
                    entry.State != Models.OperationState.Running &&
                    entry.State != Models.OperationState.Stopping)
                {
                    throw new InvalidOperationException(Strings.ErrorMessage_OperationNotRunning);
                }

                entry.State = Models.OperationState.Cancelled;
                entry.EgressRequest.CancellationTokenSource.Cancel();
                entry.EgressRequest.Dispose();

                entry.TaskCompletionSource.TrySetCanceled();
            }
        }

        public void CompleteOperation(Guid operationId, ExecutionResult<EgressResult> result)
        {
            lock (_requests)
            {
                if (!_requests.TryGetValue(operationId, out EgressEntry? entry))
                {
                    throw new InvalidOperationException(Strings.ErrorMessage_OperationNotFound);
                }

                if (entry.State != Models.OperationState.Starting &&
                    entry.State != Models.OperationState.Running &&
                    entry.State != Models.OperationState.Stopping)
                {
                    throw new InvalidOperationException(Strings.ErrorMessage_OperationNotRunning);
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

                entry.TaskCompletionSource.TrySetResult();
            }
        }

        public IEnumerable<Models.OperationSummary> GetOperations(ProcessKey? processKey, string? tags)
        {
            lock (_requests)
            {
                IEnumerable<KeyValuePair<Guid, EgressEntry>> requests = _requests;

                if (!string.IsNullOrEmpty(tags))
                {
                    ISet<string> tagsSet = Utilities.SplitTags(tags);

                    requests = requests.Where((kvp) =>
                    {
                        return tagsSet.IsSubsetOf(kvp.Value.Tags);
                    });
                }

                if (null != processKey)
                {
                    requests = requests.Where((kvp) =>
                    {
                        EgressProcessInfo processInfo = kvp.Value.EgressRequest.EgressOperation.ProcessInfo;

                        // Check that if a field is specified, it meets the conditions.
                        if (!string.IsNullOrEmpty(processKey.Value.ProcessName)
                            && processInfo.ProcessName != processKey.Value.ProcessName)
                        {
                            return false;
                        }

                        if (processKey.Value.ProcessId.HasValue
                            && processInfo.ProcessId != processKey.Value.ProcessId.Value)
                        {
                            return false;
                        }

                        if (processKey.Value.RuntimeInstanceCookie.HasValue
                            && processInfo.RuntimeInstanceCookie != processKey.Value.RuntimeInstanceCookie.Value)
                        {
                            return false;
                        }

                        return true;
                    });
                }

                return requests.Select((kvp) =>
                {
                    EgressProcessInfo processInfo = kvp.Value.EgressRequest.EgressOperation.ProcessInfo;
                    return new Models.OperationSummary
                    {
                        OperationId = kvp.Key,
                        CreatedDateTime = kvp.Value.CreatedDateTime,
                        Status = kvp.Value.State,
                        EgressProviderName = kvp.Value.EgressRequest.EgressOperation.EgressProviderName,
                        IsStoppable = kvp.Value.IsStoppable,
                        Process = processInfo != null ?
                            new Models.OperationProcessInfo
                            {
                                Name = processInfo.ProcessName,
                                ProcessId = processInfo.ProcessId,
                                Uid = processInfo.RuntimeInstanceCookie
                            } : null,
                        Tags = kvp.Value.Tags
                    };
                }).ToList();
            }
        }

        public Models.OperationStatus GetOperationStatus(Guid operationId)
        {
            lock (_requests)
            {
                if (!_requests.TryGetValue(operationId, out EgressEntry? entry))
                {
                    throw new InvalidOperationException(Strings.ErrorMessage_OperationNotFound);
                }
                EgressProcessInfo processInfo = entry.EgressRequest.EgressOperation.ProcessInfo;

                var status = new Models.OperationStatus()
                {
                    OperationId = entry.EgressRequest.OperationId,
                    Status = entry.State,
                    CreatedDateTime = entry.CreatedDateTime,
                    EgressProviderName = entry.EgressRequest.EgressOperation.EgressProviderName,
                    IsStoppable = entry.IsStoppable,
                    Process = processInfo != null ?
                        new Models.OperationProcessInfo
                        {
                            Name = processInfo.ProcessName,
                            ProcessId = processInfo.ProcessId,
                            Uid = processInfo.RuntimeInstanceCookie
                        } : null,
                    Tags = entry.Tags
                };

                if (entry.State == Models.OperationState.Succeeded)
                {
                    status.ResourceLocation = entry.ExecutionResult.Result.Value;
                }
                else if (entry.State == Models.OperationState.Failed)
                {
#nullable disable
                    status.Error = new Models.OperationError
                    {
                        Code = entry.ExecutionResult.ProblemDetails.Status?.ToString(CultureInfo.InvariantCulture),
                        Message = entry.ExecutionResult.ProblemDetails.Detail
                    };
#nullable restore
                }

                return status;
            }
        }
    }
}
