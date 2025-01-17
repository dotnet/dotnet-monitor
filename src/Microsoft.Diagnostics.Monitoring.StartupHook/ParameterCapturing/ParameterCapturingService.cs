// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.StartupHook.Monitoring;
using Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing.Eventing;
using Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing.FunctionProbes;
using Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing.Pipeline;
using Microsoft.Diagnostics.Tools.Monitor.ParameterCapturing;
using Microsoft.Diagnostics.Tools.Monitor.Profiler;
using Microsoft.Diagnostics.Tools.Monitor.StartupHook;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing
{
    internal sealed class ParameterCapturingService : BackgroundService, IParameterCapturingPipelineCallbacks, IDisposable
    {
        private long _disposedState;

        private ParameterCapturingEvents.ServiceState _serviceState;
        private string _serviceStateDetails = string.Empty;

        private readonly ParameterCapturingEventSource _eventSource = new();
        private readonly AsyncParameterCapturingEventSource? _asyncEventSource;
        private readonly ParameterCapturingPipeline? _pipeline;

        public ParameterCapturingService()
        {
            using IDisposable _ = MonitorExecutionContextTracker.MonitorScope();

            try
            {
                ArgumentNullException.ThrowIfNull(SharedInternals.MessageDispatcher);

                // Register the command callbacks (if possible) first so that dotnet-monitor
                // can be notified of any initialization errors when it tries to invoke the commands.
                SharedInternals.MessageDispatcher.RegisterCallback<StartCapturingParametersPayload>(
                    StartupHookCommand.StartCapturingParameters,
                    OnStartMessage);

                SharedInternals.MessageDispatcher.RegisterCallback<StopCapturingParametersPayload>(
                    StartupHookCommand.StopCapturingParameters,
                    OnStopMessage);

                IMethodDescriptionValidator _methodDescriptionValidator = new MethodDescriptionValidator();

                FunctionProbesManager probeManager = new();

                _pipeline = new ParameterCapturingPipeline(probeManager, this, _methodDescriptionValidator);

                _asyncEventSource = new(_eventSource);
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

        #region IParameterCapturingPipelineCallbacks
        public void CapturingStart(StartCapturingParametersPayload request, IList<MethodInfo> methods)
        {
            _eventSource.CapturingStart(request.RequestId);
        }

        public void CapturingStop(Guid requestId)
        {
            _eventSource.CapturingStop(requestId);
        }

        public void FailedToCapture(Guid requestId, ParameterCapturingEvents.CapturingFailedReason reason, string details)
        {
            _eventSource.FailedToCapture(requestId, reason, details);
        }

        public void ProbeFault(Guid requestId, InstrumentedMethod faultingMethod)
        {
            _eventSource.FailedToCapture(
                requestId,
                ParameterCapturingEvents.CapturingFailedReason.ProbeFaulted,
                string.Format(
                    CultureInfo.InvariantCulture,
                    ParameterCapturingStrings.StoppingParameterCapturingDueToProbeFault,
                    faultingMethod.MethodSignature.ModuleName,
                    faultingMethod.MethodSignature.TypeName,
                    faultingMethod.MethodSignature.MethodName
                ));
            try
            {
                _pipeline?.RequestStop(requestId);
            }
            catch
            {

            }
        }
        #endregion

        private bool IsAvailable()
        {
            return _serviceState == ParameterCapturingEvents.ServiceState.Running;
        }

        private void OnStartMessage(StartCapturingParametersPayload payload)
        {
            if (!IsAvailable() || _pipeline == null || _asyncEventSource == null)
            {
                BroadcastServiceState();
                return;
            }

            try
            {
                _pipeline.SubmitRequest(payload, new EventSourceEmittingProbes(_asyncEventSource, payload.RequestId, payload.Configuration.UseDebuggerDisplayAttribute));
            }
            catch (ArgumentException ex)
            {
                _eventSource.FailedToCapture(payload.RequestId, ParameterCapturingEvents.CapturingFailedReason.InvalidRequest, ex.Message);
            }
            catch (TooManyRequestsException ex)
            {
                if (!IsAvailable())
                {
                    BroadcastServiceState();
                }
                else
                {
                    _eventSource.FailedToCapture(payload.RequestId, ParameterCapturingEvents.CapturingFailedReason.TooManyRequests, ex.Message);
                }
            }
        }

        private void OnStopMessage(StopCapturingParametersPayload payload)
        {
            if (!IsAvailable() || _pipeline == null)
            {
                BroadcastServiceState();
                return;
            }

            try
            {
                _pipeline.RequestStop(payload.RequestId);
            }
            catch (ArgumentException)
            {
                _eventSource.UnknownRequestId(payload.RequestId);
            }
        }

        private void UnrecoverableError(Exception ex)
        {
            ChangeServiceState(ParameterCapturingEvents.ServiceState.InternalError, ex.ToString());
        }

        private void ChangeServiceState(ParameterCapturingEvents.ServiceState state, string? details = null)
        {
            _serviceState = state;
            _serviceStateDetails = details ?? string.Empty;
            BroadcastServiceState();

            if (state != ParameterCapturingEvents.ServiceState.Running)
            {
                _ = _pipeline?.TryComplete();
            }
        }

        private void BroadcastServiceState()
        {
            _eventSource.ServiceStateUpdate(_serviceState, _serviceStateDetails);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_pipeline == null)
            {
                return;
            }

            using IDisposable _ = MonitorExecutionContextTracker.MonitorScope();

            ChangeServiceState(ParameterCapturingEvents.ServiceState.Running);
            try
            {
                await _pipeline.RunAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception ex)
            {
                UnrecoverableError(ex);
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

            SharedInternals.MessageDispatcher?.UnregisterCallback(StartupHookCommand.StartCapturingParameters);
            SharedInternals.MessageDispatcher?.UnregisterCallback(StartupHookCommand.StopCapturingParameters);

            _pipeline?.Dispose();

            _asyncEventSource?.Dispose();

            base.Dispose();
        }
    }
}
