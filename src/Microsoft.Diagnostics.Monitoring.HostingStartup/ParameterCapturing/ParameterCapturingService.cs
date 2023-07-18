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
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing
{
    internal sealed class ParameterCapturingService : BackgroundService, IDisposable
    {
        private long _disposedState;

        private ParameterCapturingEvents.ServiceNotAvailableReason? _notAvailableReason;
        private string _notAvailableDetails = string.Empty;

        private readonly FunctionProbesManager? _probeManager;
        private readonly ParameterCapturingEventSource _eventSource = new();
        private readonly ILogger? _logger;

        private Channel<StartCapturingParametersPayload>? _requests;
        private Channel<bool>? _stopRequests;

        public ParameterCapturingService(IServiceProvider services)
        {
            // Register the command callbacks (if possible) first so that dotnet-monitor
            // can be notified of any initialization errors when it tries to invoke the commands.
            SharedInternals.MessageDispatcher?.RegisterCallback<StartCapturingParametersPayload>(
                IpcCommand.StartCapturingParameters,
                OnStartMessage);

            SharedInternals.MessageDispatcher?.RegisterCallback<EmptyPayload>(
                IpcCommand.StopCapturingParameters,
                OnStopMessage);

            try
            {
                _logger = services.GetService<ILogger<ParameterCapturingService>>();
                if (_logger == null)
                {
                    throw new NotSupportedException(ParameterCapturingStrings.FeatureUnsupported_NoLogger);
                }

                ArgumentNullException.ThrowIfNull(SharedInternals.MessageDispatcher);


                //
                // Request processing overview:
                //
                // - Incoming Requests
                //   - dotnet-monitor will properly rate limit requests so there will never be 2 start requests at any given time.
                //   - Our monitor message processing (start/stop commands) happens serially.
                //   - Our monitor message processing simply ACKs a command if there's nothing immediatly wrong and queues the
                //     work to happen on our background service.
                // - Request Handling
                //  - Happens on our background service.
                //  - The background service may stop at any given point if an unrecoverable error occurs. In this scenario,
                //    dotnet-monitor is notified of the error, and all future requests will short-circuit and notify
                //    dotnet-monitor that the parameter capturing service is unavailable.
                //
                _requests = Channel.CreateBounded<StartCapturingParametersPayload>(new BoundedChannelOptions(capacity: 1)
                {
                    FullMode = BoundedChannelFullMode.DropWrite
                });
                _stopRequests = Channel.CreateBounded<bool>(new BoundedChannelOptions(capacity: 1)
                {
                    FullMode = BoundedChannelFullMode.DropWrite
                });

                _probeManager = new FunctionProbesManager(new LogEmittingProbes(_logger));
            }
            catch (Exception ex)
            {
                UnrecoverableError(ex);
            }
        }

        private bool IsAvailable()
        {
            return _notAvailableReason == null;
        }

        private void OnStartMessage(StartCapturingParametersPayload payload)
        {
            if (payload.Methods.Length == 0)
            {
                _eventSource.FailedToCapture(new ArgumentException(nameof(payload.Methods)));
                return;
            }

            if (_requests?.Writer.TryWrite(payload) != true)
            {
                if (!IsAvailable())
                {
                    _eventSource.ServiceNotAvailable(_notAvailableReason!.Value, _notAvailableDetails);
                    return;
                }
                else
                {
                    // The channel is full, which should never happen if dotnet-monitor is properly rate limiting requests.
                    _eventSource.FailedToCapture(
                        ParameterCapturingEvents.CapturingFailedReason.TooManyRequests,
                        ParameterCapturingStrings.TooManyRequestsErrorMessage);
                }
            }
        }

        private void OnStopMessage(EmptyPayload _)
        {
            if (_stopRequests?.Writer.TryWrite(true) != true)
            {
                if (IsAvailable())
                {
                    _eventSource.ServiceNotAvailable(_notAvailableReason!.Value, _notAvailableDetails);
                    return;
                }
                else
                {
                    // The channel is full which is OK as stop requests aren't tied to a specific operation.
                }
            }
        }

        private void StartCapturing(StartCapturingParametersPayload request)
        {
            if (!IsAvailable())
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
                _logger!.LogWarning(ex.Message);
                throw ex;
            }

            _probeManager!.StartCapturing(methods);
            _logger!.LogInformation(
                ParameterCapturingStrings.StartParameterCapturingFormatString,
                request.Duration,
                request.Methods.Length);
        }

        private void StopCapturing()
        {
            if (!IsAvailable())
            {
                return;
            }

            _logger!.LogInformation(ParameterCapturingStrings.StopParameterCapturing);
            _probeManager!.StopCapturing();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!IsAvailable())
            {
                return;
            }

            void tryToStopCapturing()
            {
                try
                {
                    StopCapturing();
                    _eventSource.CapturingStop();
                }
                catch (Exception ex)
                {
                    // We're in a faulted state from an internal exception so there's
                    // nothing else that can be safely done for the remainder of the app's lifetime.
                    UnrecoverableError(ex);
                }
            }

            stoppingToken.Register(tryToStopCapturing);
            while (IsAvailable() && !stoppingToken.IsCancellationRequested)
            {
                StartCapturingParametersPayload req = await _requests!.Reader.ReadAsync(stoppingToken);
                try
                {
                    StartCapturing(req);
                    _eventSource.CapturingStart();
                }
                catch (Exception ex)
                {
                    _eventSource.FailedToCapture(ex);
                    continue;
                }

                using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                Task stopSignalTask = _stopRequests!.Reader.WaitToReadAsync(cts.Token).AsTask();
                _ = Task.WhenAny(stopSignalTask, Task.Delay(req.Duration, cts.Token)).WaitAsync(stoppingToken).ConfigureAwait(false);

                // Signal the other stop condition tasks to cancel
                cts.Cancel();

                // Drain the stop request (if present)
                _ = _stopRequests.Reader.TryRead(out _);

                tryToStopCapturing();
            }
        }

        private void UnrecoverableError(Exception ex)
        {
            if (ex is NotSupportedException)
            {
                _notAvailableReason = ParameterCapturingEvents.ServiceNotAvailableReason.NotSupported;
                _notAvailableDetails = ex.Message;
            }
            else
            {
                _notAvailableReason = ParameterCapturingEvents.ServiceNotAvailableReason.InternalError;
                _notAvailableDetails = ex.ToString();
            }

            _eventSource.ServiceNotAvailable(_notAvailableReason!.Value, _notAvailableDetails);

            _ = _requests?.Writer.TryComplete();
            _ = _stopRequests?.Writer.TryComplete();
        }

        public override void Dispose()
        {
            if (!DisposableHelper.CanDispose(ref _disposedState))
                return;

            SharedInternals.MessageDispatcher?.UnregisterCallback(IpcCommand.StartCapturingParameters);
            SharedInternals.MessageDispatcher?.UnregisterCallback(IpcCommand.StopCapturingParameters);

            try
            {
                _probeManager?.Dispose();
            }
            catch
            {

            }

            base.Dispose();
        }
    }
}
