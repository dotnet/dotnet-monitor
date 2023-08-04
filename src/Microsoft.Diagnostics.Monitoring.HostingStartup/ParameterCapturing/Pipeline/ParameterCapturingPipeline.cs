﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.FunctionProbes;
using Microsoft.Diagnostics.Monitoring.StartupHook.MonitorMessageDispatcher.Models;
using Microsoft.Diagnostics.Tools.Monitor.ParameterCapturing;
using Microsoft.Diagnostics.Tools.Monitor.Profiler;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.Pipeline
{
    internal sealed class ParameterCapturingPipeline : IDisposable
    {
        private sealed class CapturingRequest
        {
            public CapturingRequest(StartCapturingParametersPayload payload)
            {
                Payload = payload ?? throw new ArgumentNullException(nameof(payload));
                StopRequest = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            public StartCapturingParametersPayload Payload { get; }
            public TaskCompletionSource StopRequest { get; }
        }

        private readonly IFunctionProbesManager _probeManager;
        private readonly IParameterCapturingPipelineCallbacks _callbacks;
        private readonly Channel<CapturingRequest> _requestQueue;
        private readonly ConcurrentDictionary<Guid, CapturingRequest> _allRequests = new();

        public ParameterCapturingPipeline(IFunctionProbesManager probeManager, IParameterCapturingPipelineCallbacks callbacks)
        {
            _probeManager = probeManager;
            _callbacks = callbacks;

            _requestQueue = Channel.CreateBounded<CapturingRequest>(new BoundedChannelOptions(capacity: 1)
            {
                FullMode = BoundedChannelFullMode.DropWrite
            });
        }

        public async Task RunAsync(CancellationToken stoppingToken)
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

                _probeManager.StopCapturing();

                _callbacks.CapturingStop(request.Payload.RequestId);
                _ = _allRequests.TryRemove(request.Payload.RequestId, out _);
            }
        }

        // Private method for work that happens inside the pipeline's RunAsync
        // so use callbacks instead of throwing exceptions.
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
                    throw ex;
                }

                _probeManager.StartCapturing(methods);
                _callbacks.CapturingStart(request, methods);

                return true;
            }
            catch (UnresolvedMethodsExceptions ex)
            {
                _callbacks.FailedToCapture(
                    request.RequestId,
                    ParameterCapturingEvents.CapturingFailedReason.UnresolvedMethods,
                    ex.Message);
            }
            catch (Exception ex)
            {
                _callbacks.FailedToCapture(
                    request.RequestId,
                    ParameterCapturingEvents.CapturingFailedReason.InternalError,
                    ex.ToString());
            }

            return false;
        }

        public bool TryComplete()
        {
            return _requestQueue.Writer.TryComplete();
        }

        public void SubmitRequest(StartCapturingParametersPayload payload)
        {
            if (payload.Methods.Length == 0)
            {
                throw new ArgumentException(nameof(payload.Methods));
            }

            CapturingRequest request = new(payload);
            if (!_allRequests.TryAdd(payload.RequestId, request))
            {
                throw new ArgumentException(nameof(payload.RequestId));
            }

            if (!_requestQueue.Writer.TryWrite(request))
            {
                _ = request.StopRequest.TrySetCanceled();
                _ = _allRequests.TryRemove(payload.RequestId, out _);

                throw new TooManyRequestsException(ParameterCapturingStrings.TooManyRequestsErrorMessage);
            }
        }

        public void RequestStop(Guid requestId)
        {
            if (!_allRequests.TryGetValue(requestId, out CapturingRequest? request))
            {
                throw new ArgumentException(nameof(requestId));
            }

            _ = request.StopRequest?.TrySetResult();
        }

        public void Dispose()
        {
            _probeManager.Dispose();
        }
    }
}
