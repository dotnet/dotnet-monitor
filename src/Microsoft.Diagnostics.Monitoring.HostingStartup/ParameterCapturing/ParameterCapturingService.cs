// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.Eventing;
using Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.FunctionProbes;
using Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.Pipeline;
using Microsoft.Diagnostics.Monitoring.StartupHook;
using Microsoft.Diagnostics.Tools.Monitor.ParameterCapturing;
using Microsoft.Diagnostics.Tools.Monitor.Profiler;
using Microsoft.Diagnostics.Tools.Monitor.StartupHook;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing
{
    internal sealed class ParameterCapturingService : BackgroundService, IParameterCapturingPipelineCallbacks, IDisposable
    {
        private long _disposedState;

        private ParameterCapturingEvents.ServiceState _serviceState;
        private string _serviceStateDetails = string.Empty;

        private readonly ParameterCapturingEventSource _eventSource = new();
        private readonly ParameterCapturingPipeline? _pipeline;
        private readonly ParameterCapturingLogger? _parameterCapturingLogger;

        private readonly ILogger? _logger;

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

                _logger = services.GetService<ILogger<DotnetMonitor.ParameterCapture.Service>>()
                    ?? throw new NotSupportedException(ParameterCapturingStrings.FeatureUnsupported_NoLogger);

                ILogger userLogger = services.GetService<ILogger<DotnetMonitor.ParameterCapture.UserCode>>()
                    ?? throw new NotSupportedException(ParameterCapturingStrings.FeatureUnsupported_NoLogger);

                ILogger systemLogger = services.GetService<ILogger<DotnetMonitor.ParameterCapture.SystemCode>>()
                    ?? throw new NotSupportedException(ParameterCapturingStrings.FeatureUnsupported_NoLogger);

                _parameterCapturingLogger = new(userLogger, systemLogger);
                FunctionProbesManager probeManager = new(new LogEmittingProbes(_parameterCapturingLogger));

                _pipeline = new ParameterCapturingPipeline(probeManager, this);
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
            _logger?.LogInformation(
                ParameterCapturingStrings.StartParameterCapturingFormatString,
                request.Duration,
                methods.Count);
        }

        public void CapturingStop(Guid requestId)
        {
            _eventSource.CapturingStop(requestId);
            _logger?.LogInformation(ParameterCapturingStrings.StopParameterCapturing);
        }

        public void FailedToCapture(Guid requestId, ParameterCapturingEvents.CapturingFailedReason reason, string details)
        {
            _eventSource.FailedToCapture(requestId, reason, details);
            if (reason == ParameterCapturingEvents.CapturingFailedReason.UnresolvedMethods)
            {
                _logger?.LogWarning(details);
            }
        }

        public void ProbeFault(Guid requestId, InstrumentedMethod faultingMethod)
        {
            // TODO: Report back this fault on ParameterCapturingEventSource. 
            _logger?.LogWarning(ParameterCapturingStrings.StoppingParameterCapturingDueToProbeFault, faultingMethod.MethodWithParametersTemplateString);

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
            if (!IsAvailable() || _pipeline == null)
            {
                BroadcastServiceState();
                return;
            }

            try
            {
                _pipeline.SubmitRequest(payload);
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

            SharedInternals.MessageDispatcher?.UnregisterCallback(IpcCommand.StartCapturingParameters);
            SharedInternals.MessageDispatcher?.UnregisterCallback(IpcCommand.StopCapturingParameters);

            _pipeline?.Dispose();
            _parameterCapturingLogger?.Dispose();

            base.Dispose();
        }
    }
}
