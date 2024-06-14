// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing.FunctionProbes;
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

namespace Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing.Pipeline
{
    internal sealed class ParameterCapturingPipeline : IDisposable
    {
        private sealed class CapturingRequest
        {
            public CapturingRequest(StartCapturingParametersPayload payload, IFunctionProbes probes)
            {
                Payload = payload;
                StopRequest = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

                Probes = payload.Configuration.CaptureLimit.HasValue
                    ? new CaptureLimitPolicyProbes(probes, payload.Configuration.CaptureLimit.Value, StopRequest)
                    : probes;
            }

            public StartCapturingParametersPayload Payload { get; }

            public IFunctionProbes Probes { get; }

            public TaskCompletionSource StopRequest { get; }
        }

        private readonly IFunctionProbesManager _probeManager;
        private readonly IMethodDescriptionValidator _methodDescriptionValidator;
        private readonly IParameterCapturingPipelineCallbacks _callbacks;
        private readonly Channel<CapturingRequest> _requestQueue;
        private readonly ConcurrentDictionary<Guid, CapturingRequest> _allRequests = new();

        public ParameterCapturingPipeline(IFunctionProbesManager probeManager, IParameterCapturingPipelineCallbacks callbacks, IMethodDescriptionValidator methodDescriptionValidator)
        {
            _probeManager = probeManager;
            _methodDescriptionValidator = methodDescriptionValidator;
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

                void onFault(object? sender, InstrumentedMethod faultingMethod)
                {
                    _callbacks.ProbeFault(request.Payload.RequestId, faultingMethod);
                }

                try
                {
                    _probeManager.OnProbeFault += onFault;

                    if (!await TryStartCapturingAsync(request, stoppingToken).ConfigureAwait(false))
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

                    //
                    // NOTE:
                    // StopCapturingAsync will request a stop regardless of if the stoppingToken is set.
                    // While we don't support the host & services reloading, the above behavior will ensure
                    // that we don't leave the app in a potentially bad state on a reload.
                    //
                    // See: https://github.com/dotnet/dotnet-monitor/issues/5170
                    //
                    await _probeManager.StopCapturingAsync(stoppingToken).ConfigureAwait(false);

                    _callbacks.CapturingStop(request.Payload.RequestId);
                    _ = _allRequests.TryRemove(request.Payload.RequestId, out _);
                }
                finally
                {
                    _probeManager.OnProbeFault -= onFault;
                }
            }
        }

        // Private method for work that happens inside the pipeline's RunAsync
        // so use callbacks instead of throwing exceptions.
        private async Task<bool> TryStartCapturingAsync(CapturingRequest request, CancellationToken token)
        {
            try
            {
                MethodResolver resolver = new();
                List<MethodInfo> methods = new(request.Payload.Configuration.Methods.Length);
                List<MethodDescription> methodsFailedToResolve = new();

                for (int i = 0; i < request.Payload.Configuration.Methods.Length; i++)
                {
                    MethodDescription methodDescription = request.Payload.Configuration.Methods[i];

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

                await _probeManager.StartCapturingAsync(methods, request.Probes, token).ConfigureAwait(false);
                _callbacks.CapturingStart(request.Payload, methods);

                return true;
            }
            catch (UnresolvedMethodsExceptions ex)
            {
                _callbacks.FailedToCapture(
                    request.Payload.RequestId,
                    ParameterCapturingEvents.CapturingFailedReason.UnresolvedMethods,
                    ex.Message);
            }
            catch (Exception ex)
            {
                _callbacks.FailedToCapture(
                    request.Payload.RequestId,
                    ParameterCapturingEvents.CapturingFailedReason.InternalError,
                    ex.ToString());
            }

            return false;
        }

        public bool TryComplete()
        {
            return _requestQueue.Writer.TryComplete();
        }

        public void SubmitRequest(StartCapturingParametersPayload payload, IFunctionProbes probes)
        {
            ArgumentNullException.ThrowIfNull(payload.Configuration);

            if (payload.Configuration.Methods.Length == 0)
            {
                throw new ArgumentException(nameof(payload.Configuration.Methods));
            }

            if (payload.Configuration.CaptureLimit.HasValue && payload.Configuration.CaptureLimit.Value <= 0)
            {
                throw new ArgumentException(nameof(payload.Configuration.CaptureLimit));
            }

            List<MethodDescription> _deniedMethodDescriptions = new();
            foreach (MethodDescription methodDescription in payload.Configuration.Methods)
            {
                if (!_methodDescriptionValidator.IsMethodDescriptionAllowed(methodDescription))
                {
                    _deniedMethodDescriptions.Add(methodDescription);
                }
            }

            if (_deniedMethodDescriptions.Count > 0)
            {
                throw new DeniedMethodsException(_deniedMethodDescriptions);
            }

            CapturingRequest request = new(payload, probes);
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
