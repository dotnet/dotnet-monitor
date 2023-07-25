// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.Eventing;
using Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.FunctionProbes;
using Microsoft.Diagnostics.Monitoring.StartupHook;
using Microsoft.Diagnostics.Monitoring.StartupHook.MonitorMessageDispatcher.Models;
using Microsoft.Diagnostics.Tools.Monitor.ParameterCapturing;
using Microsoft.Diagnostics.Tools.Monitor.StartupHook;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
    internal sealed class ParameterCapturingService : BackgroundService, IDisposable
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

        private sealed class InitializedState : IDisposable
        {
            public InitializedState(IServiceProvider services)
            {
                Logger = services.GetService<ILogger<ParameterCapturingService>>()
                    ?? throw new NotSupportedException(ParameterCapturingStrings.FeatureUnsupported_NoLogger);

                ProbeManager = new FunctionProbesManager(new LogEmittingProbes(Logger));

                RequestQueue = Channel.CreateBounded<CapturingRequest>(new BoundedChannelOptions(capacity: 1)
                {
                    FullMode = BoundedChannelFullMode.DropWrite
                });
            }

            public FunctionProbesManager ProbeManager { get; }
            public ILogger Logger { get; }
            public Channel<CapturingRequest> RequestQueue { get; }
            public ConcurrentDictionary<Guid, CapturingRequest> AllRequests { get; } = new();

            public void Dispose()
            {
                ProbeManager.Dispose();
            }
        }

        private long _disposedState;

        private ParameterCapturingEvents.ServiceState _serviceState;
        private string _serviceStateDetails = string.Empty;

        private readonly InitializedState? _initializedState;

        private readonly ParameterCapturingEventSource _eventSource = new();

        public ParameterCapturingService(IServiceProvider services)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(SharedInternals.MessageDispatcher);

                // Register the command callbacks (if possible) first so that dotnet-monitor
                // can be notified of any initialization errors when it tries to invoke the commands.
                SharedInternals.MessageDispatcher.RegisterCallback<StartCapturingParametersPayload>(
                    IpcCommand.StartCapturingParameters,
                    OnStartMessage);

                SharedInternals.MessageDispatcher.RegisterCallback<StopCapturingParametersPayload>(
                    IpcCommand.StopCapturingParameters,
                    OnStopMessage);

                _initializedState = new InitializedState(services);
            }
            catch (NotSupportedException ex)
            {
                ChangeServiceState(ParameterCapturingEvents.ServiceState.NotSupported, ex.Message);
            }
            catch (Exception ex)
            {
                UnrecoverableError(ex);
            }
        }

        private bool IsAvailable()
        {
            return _serviceState == ParameterCapturingEvents.ServiceState.Running;
        }

        private void OnStartMessage(StartCapturingParametersPayload payload)
        {
            if (!IsAvailable() || _initializedState == null)
            {
                BroadcastServiceState();
                return;
            }

            if (payload.Methods.Length == 0)
            {
                _eventSource.FailedToCapture(
                    payload.RequestId,
                    ParameterCapturingEvents.CapturingFailedReason.InvalidRequest,
                    nameof(payload.Methods));
                return;
            }

            CapturingRequest request = new(payload);
            if (!_initializedState.AllRequests.TryAdd(payload.RequestId, request))
            {
                _eventSource.FailedToCapture(
                    payload.RequestId,
                    ParameterCapturingEvents.CapturingFailedReason.InvalidRequest,
                    nameof(payload.RequestId));
                return;
            }

            if (!_initializedState.RequestQueue.Writer.TryWrite(request))
            {
                _ = request.StopRequest.TrySetCanceled();
                _ = _initializedState.AllRequests.TryRemove(payload.RequestId, out _);

                if (!IsAvailable())
                {
                    BroadcastServiceState();
                }
                else
                {
                    // The channel is full, which should never happen if dotnet-monitor is properly rate limiting requests.
                    _eventSource.FailedToCapture(
                        payload.RequestId,
                        ParameterCapturingEvents.CapturingFailedReason.TooManyRequests,
                        ParameterCapturingStrings.TooManyRequestsErrorMessage);
                }
            }
        }

        private void OnStopMessage(StopCapturingParametersPayload payload)
        {
            if (!IsAvailable() || _initializedState == null)
            {
                BroadcastServiceState();
                return;
            }

            if (!_initializedState.AllRequests.TryGetValue(payload.RequestId, out CapturingRequest? request))
            {
                _eventSource.UnknownRequestId(payload.RequestId);
                return;
            }

            _ = request.StopRequest.TrySetResult();
        }

        private bool TryStartCapturing(StartCapturingParametersPayload request)
        {
            try
            {
                // _initializedState will never be null here, this is added to avoid needing to use _initializedState! everywhere
                if (_initializedState == null)
                {
                    throw new InvalidOperationException();
                }

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
                    _initializedState.Logger.LogWarning(ex.Message);
                    throw ex;
                }

                _initializedState.ProbeManager.StartCapturing(methods);
                _eventSource.CapturingStart(request.RequestId);
                _initializedState.Logger.LogInformation(
                    ParameterCapturingStrings.StartParameterCapturingFormatString,
                    request.Duration,
                    request.Methods.Length);

                return true;
            }
            catch (UnresolvedMethodsExceptions ex)
            {
                _eventSource.FailedToCapture(
                    request.RequestId,
                    ParameterCapturingEvents.CapturingFailedReason.UnresolvedMethods,
                    ex.Message);
            }
            catch (Exception ex)
            {
                _eventSource.FailedToCapture(request.RequestId, ex);
            }

            return false;
        }

        private bool TryStopCapturing(Guid requestId)
        {
            try
            {
                // _initializedState will never be null here, this is added to avoid needing to use _initializedState! everywhere
                if (_initializedState == null)
                {
                    throw new InvalidOperationException();
                }

                _initializedState.Logger.LogInformation(ParameterCapturingStrings.StopParameterCapturing);
                _initializedState.ProbeManager.StopCapturing();
                _eventSource.CapturingStop(requestId);

                return true;
            }
            catch (Exception ex)
            {
                //
                // We're in a faulted state from an internal exception so there's
                // nothing else that can be safely done for the remainder of the app's lifetime.
                //
                // The probe method cache will have been cleared by the _probeManager in this situation
                // so while the probes may still be installed they will be no-ops.
                //
                UnrecoverableError(ex);
            }

            return false;
        }

        private void UnrecoverableError(Exception ex)
        {
            ChangeServiceState(ParameterCapturingEvents.ServiceState.InternalError, ex.ToString());
        }

        private void ChangeServiceState(ParameterCapturingEvents.ServiceState state, string? details = null)
        {
            _serviceState = state;
            _serviceStateDetails = details ?? string.Empty;
            if (state != ParameterCapturingEvents.ServiceState.Running)
            {
                _ = _initializedState?.RequestQueue.Writer.TryComplete();
            }

            BroadcastServiceState();
        }

        private void BroadcastServiceState()
        {
            _eventSource.ServiceStateUpdate(_serviceState, _serviceStateDetails);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_initializedState == null)
            {
                return;
            }

            ChangeServiceState(ParameterCapturingEvents.ServiceState.Running);
            while (IsAvailable() && !stoppingToken.IsCancellationRequested)
            {
                CapturingRequest request = await _initializedState.RequestQueue.Reader.ReadAsync(stoppingToken);
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

                _ = TryStopCapturing(request.Payload.RequestId);
                _ = _initializedState.AllRequests.TryRemove(request.Payload.RequestId, out _);
            }

            if (IsAvailable())
            {
                ChangeServiceState(ParameterCapturingEvents.ServiceState.Stopped);
            }
        }

        public override void Dispose()
        {
            if (!DisposableHelper.CanDispose(ref _disposedState))
                return;

            SharedInternals.MessageDispatcher?.UnregisterCallback(IpcCommand.StartCapturingParameters);
            SharedInternals.MessageDispatcher?.UnregisterCallback(IpcCommand.StopCapturingParameters);

            try
            {
                _initializedState?.Dispose();
            }
            catch
            {

            }

            base.Dispose();
        }
    }
}
