// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.Eventing;
using Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.FunctionProbes;
using Microsoft.Diagnostics.Monitoring.StartupHook;
using Microsoft.Diagnostics.Tools.Monitor.ParameterCapturing;
using Microsoft.Diagnostics.Tools.Monitor.Profiler;
using Microsoft.Diagnostics.Tools.Monitor.StartupHook;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing
{
    internal sealed class ParameterCapturingService : BackgroundService, IDisposable
    {
        private long _disposedState;

        private ParameterCapturingEvents.ServiceState _serviceState;
        private string _serviceStateDetails = string.Empty;

        private readonly ParameterCapturingPipeline? _pipeline;

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

                ILogger logger = services.GetService<ILogger<DotnetMonitor.ParameterCapture.UserCode>>()
                    ?? throw new NotSupportedException(ParameterCapturingStrings.FeatureUnsupported_NoLogger);

                FunctionProbesManager probeManager = new(new LogEmittingProbes(logger));

                _pipeline = new ParameterCapturingPipeline(logger, probeManager);
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
            if (!IsAvailable() || _pipeline == null)
            {
                BroadcastServiceState();
                return;
            }

            _ = _pipeline.TrySubmitRequest(payload);
        }

        private void OnStopMessage(StopCapturingParametersPayload payload)
        {
            if (!IsAvailable() || _pipeline == null)
            {
                BroadcastServiceState();
                return;
            }

            _ = _pipeline.TryStopRequest(payload.RequestId);
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
                _ = _pipeline?.TryComplete();
            }

            BroadcastServiceState();
        }

        private void BroadcastServiceState()
        {
            ParameterCapturingEventSource.Log.ServiceStateUpdate(_serviceState, _serviceStateDetails);
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

            base.Dispose();
        }
    }
}
