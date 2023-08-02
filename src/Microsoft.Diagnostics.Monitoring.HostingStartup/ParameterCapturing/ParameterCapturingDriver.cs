// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.Eventing;
using Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.FunctionProbes;
using Microsoft.Diagnostics.Monitoring.StartupHook.MonitorMessageDispatcher.Models;
using Microsoft.Diagnostics.Tools.Monitor.ParameterCapturing;
using Microsoft.Diagnostics.Tools.Monitor.Profiler;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing
{
    internal sealed class CapturingRequest
    {
        public CapturingRequest(StartCapturingParametersPayload payload)
        {
            Payload = payload ?? throw new ArgumentNullException(nameof(payload));
            StopRequest = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public StartCapturingParametersPayload Payload { get; }
        public TaskCompletionSource StopRequest { get; }
    }


    internal sealed class ParameterCapturingDriver : IDisposable
    {
        private readonly IFunctionProbesManager _probeManager;
        private readonly ILogger _logger;
        private readonly Channel<CapturingRequest> _requestQueue;
        private readonly ConcurrentDictionary<Guid, CapturingRequest> _allRequests = new();

        public ParameterCapturingDriver(ILogger logger, IFunctionProbesManager probeManager)
        {
            _logger = logger;
            _probeManager = probeManager;

            _requestQueue = Channel.CreateBounded<CapturingRequest>(new BoundedChannelOptions(capacity: 1)
            {
                FullMode = BoundedChannelFullMode.DropWrite
            });
        }

        private bool TryStartCapturing(StartCapturingParametersPayload request)
        {
            try
            {
                MethodResolver resolver = new();
                List<MethodInfo> methods = new(request.Methods.Length);
                List<MethodDescription> methodsFailedToResolve = new();

                for (int i = 0; i < request.Methods.Length; i++)
                {
                    MethodDescription methodDescription = request.Methods[i];

                    List<MethodInfo> resolvedMethods = resolver.ResolveMethodDescription(methodDescription);
                    if (resolvedMethods.Count == 0)
                    {
                        methodsFailedToResolve.Add(methodDescription);
                    }

                    methods.AddRange(resolvedMethods);
                }

                if (methodsFailedToResolve.Count > 0)
                {
                    UnresolvedMethodsExceptions ex = new(methodsFailedToResolve);
                    _logger.LogWarning(ex.Message);
                    throw ex;
                }

                _probeManager.StartCapturing(methods);
                ParameterCapturingEventSource.Instance.CapturingStart(request.RequestId);
                _logger.LogInformation(
                    ParameterCapturingStrings.StartParameterCapturingFormatString,
                    request.Duration,
                    methods.Count);

                return true;
            }
            catch (UnresolvedMethodsExceptions ex)
            {
                ParameterCapturingEventSource.Instance.FailedToCapture(
                    request.RequestId,
                    ParameterCapturingEvents.CapturingFailedReason.UnresolvedMethods,
                    ex.Message);
            }
            catch (Exception ex)
            {
                ParameterCapturingEventSource.Instance.FailedToCapture(request.RequestId, ex);
            }

            return false;
        }

        private void StopCapturing(Guid requestId)
        {
            _logger.LogInformation(ParameterCapturingStrings.StopParameterCapturing);
            _probeManager.StopCapturing();
            ParameterCapturingEventSource.Instance.CapturingStop(requestId);
        }

        public bool TryComplete()
        {
            return _requestQueue.Writer.TryComplete();
        }

        public bool TrySubmitRequest(CapturingRequest request)
        {
            if (request.Payload.Methods.Length == 0)
            {
                ParameterCapturingEventSource.Instance.FailedToCapture(
                    request.Payload.RequestId,
                    ParameterCapturingEvents.CapturingFailedReason.InvalidRequest,
                    nameof(request.Payload.Methods));

                return false;
            }

            if (!_allRequests.TryAdd(request.Payload.RequestId, request))
            {
                ParameterCapturingEventSource.Instance.FailedToCapture(
                   request.Payload.RequestId,
                   ParameterCapturingEvents.CapturingFailedReason.InvalidRequest,
                   nameof(request.Payload.RequestId));

                return false;
            }

            if (!_requestQueue.Writer.TryWrite(request))
            {
                _ = request.StopRequest.TrySetCanceled();
                _ = _allRequests.TryRemove(request.Payload.RequestId, out _);

                ParameterCapturingEventSource.Instance.FailedToCapture(
                    request.Payload.RequestId,
                    ParameterCapturingEvents.CapturingFailedReason.TooManyRequests,
                    ParameterCapturingStrings.TooManyRequestsErrorMessage);

                return false;
            }

            return true;
        }

        public bool TryStopRequest(Guid requestId)
        {
            if (!_allRequests.TryGetValue(requestId, out CapturingRequest? request))
            {
                return false;
            }

            _ = request.StopRequest?.TrySetResult();

            return true;
        }

        public async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                CapturingRequest request = await _requestQueue.Reader.ReadAsync(stoppingToken);
                if (!TryStartCapturing(request.Payload))
                {
                    continue;
                }

                using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                cts.CancelAfter(request.Payload.Duration);

                try
                {
                    await request.StopRequest.Task.WaitAsync(cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {

                }

                StopCapturing(request.Payload.RequestId);
                _ = _allRequests.TryRemove(request.Payload.RequestId, out _);
            }
        }

        public void Dispose()
        {
            _probeManager.Dispose();
        }
    }
}
