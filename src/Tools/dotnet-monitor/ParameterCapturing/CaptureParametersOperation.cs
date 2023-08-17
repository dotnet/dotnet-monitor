// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tools.Monitor.HostingStartup;
using Microsoft.Diagnostics.Tools.Monitor.Profiler;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.ParameterCapturing
{
    internal sealed class CaptureParametersOperation : IInProcessOperation
    {
        private readonly ProfilerChannel _profilerChannel;
        private readonly IEndpointInfo _endpointInfo;
        private readonly ILogger _logger;
        private readonly MethodDescription[] _methods;
        private readonly TimeSpan _duration;

        private readonly Guid _requestId;

        private readonly TaskCompletionSource _capturingStoppedCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _capturingStartedCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public CaptureParametersOperation(IEndpointInfo endpointInfo, ProfilerChannel profilerChannel, ILogger logger, MethodDescription[] methods, TimeSpan duration)
        {
            _profilerChannel = profilerChannel;
            _endpointInfo = endpointInfo;
            _logger = logger;
            _methods = methods;
            _duration = duration;

            _requestId = Guid.NewGuid();
        }

        public static bool DoesEndpointRuntimeSupportParameterCapturing(IEndpointInfo endpointInfo)
        {
            return endpointInfo.RuntimeVersion != null && endpointInfo.RuntimeVersion.Major >= 7;
        }

        private async Task<bool> CanEndpointProcessRequestsAsync(CancellationToken token)
        {
            DiagnosticsClient client = new(_endpointInfo.Endpoint);

            IDictionary<string, string> env = await client.GetProcessEnvironmentAsync(token);

            if (!env.TryGetValue(InProcessFeaturesIdentifiers.EnvironmentVariables.AvailableInfrastructure.ManagedMessaging, out string isManagedMessagingAvailable) ||
                !env.TryGetValue(InProcessFeaturesIdentifiers.EnvironmentVariables.AvailableInfrastructure.HostingStartup, out string isHostingStartupAvailable))
            {
                return false;
            }

            return ToolIdentifiers.IsEnvVarValueEnabled(isManagedMessagingAvailable) && ToolIdentifiers.IsEnvVarValueEnabled(isHostingStartupAvailable);
        }

        public async Task ExecuteAsync(TaskCompletionSource<object> startCompletionSource, CancellationToken token)
        {
            if (!DoesEndpointRuntimeSupportParameterCapturing(_endpointInfo))
            {
                throw new MonitoringException(Strings.ErrorMessage_ParameterCapturingRequiresAtLeastNet7);
            }

            // Check if the endpoint is capable of responding to our requests
            if (!await CanEndpointProcessRequestsAsync(token))
            {
                throw new MonitoringException(Strings.ErrorMessage_ParameterCapturingNotAvailable);
            }

            EventParameterCapturingPipelineSettings settings = new()
            {
                Duration = Timeout.InfiniteTimeSpan
            };
            settings.OnStartedCapturing += OnStartedCapturing;
            settings.OnStoppedCapturing += OnStoppedCapturing;
            settings.OnCapturingFailed += OnCapturingFailed;
            settings.OnServiceStateUpdate += OnServiceStateUpdate;
            settings.OnUnknownRequestId += OnUnknownRequestId;

            await using EventParameterCapturingPipeline eventTracePipeline = new(_endpointInfo.Endpoint, settings);
            Task runPipelineTask = eventTracePipeline.StartAsync(token);

            await _profilerChannel.SendMessage(
                _endpointInfo,
                new JsonProfilerMessage(IpcCommand.StartCapturingParameters, new StartCapturingParametersPayload
                {
                    RequestId = _requestId,
                    Duration = _duration,
                    Methods = _methods
                }),
                token);

            try
            {
                await _capturingStartedCompletionSource.Task.WaitAsync(token).ConfigureAwait(false);
                startCompletionSource?.TrySetResult(null);

                await _capturingStoppedCompletionSource.Task.WaitAsync(token).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex)
            {
                _ = startCompletionSource?.TrySetException(ex);
                _ = _capturingStartedCompletionSource.TrySetCanceled(token);
                _ = _capturingStoppedCompletionSource.TrySetCanceled(token);

                using CancellationTokenSource stopCancellationToken = new(TimeSpan.FromSeconds(30));
                await StopAsync(stopCancellationToken.Token).SafeAwait();
                throw;
            }
        }

        private void OnStartedCapturing(object sender, Guid requestId)
        {
            if (requestId != _requestId)
            {
                return;
            }

            _ = _capturingStartedCompletionSource.TrySetResult();
        }

        private void OnStoppedCapturing(object sender, Guid requestId)
        {
            if (requestId != _requestId)
            {
                return;
            }

            _ = _capturingStoppedCompletionSource.TrySetResult();
        }

        private void OnCapturingFailed(object sender, CapturingFailedArgs args)
        {
            if (args.RequestId != _requestId)
            {
                return;
            }

            Exception ex;
            switch (args.Reason)
            {
                case ParameterCapturingEvents.CapturingFailedReason.UnresolvedMethods:
                case ParameterCapturingEvents.CapturingFailedReason.InvalidRequest:
                    ex = new MonitoringException(args.Details);
                    break;
                default:
                    ex = new InvalidOperationException(args.Details);
                    break;
            }

            _ = _capturingStartedCompletionSource.TrySetException(ex);
        }

        private void OnServiceStateUpdate(object sender, ServiceStateUpdateArgs args)
        {
            Exception ex;
            switch (args.ServiceState)
            {
                case ParameterCapturingEvents.ServiceState.Running:
                    return;
                case ParameterCapturingEvents.ServiceState.NotSupported:
                    ex = new MonitoringException(args.Details);
                    break;
                default:
                    ex = new InvalidOperationException(args.Details);
                    break;
            }

            _ = _capturingStartedCompletionSource.TrySetException(ex);
            _ = _capturingStoppedCompletionSource.TrySetException(ex);
        }

        private void OnUnknownRequestId(object sender, Guid requestId)
        {
            if (requestId != _requestId)
            {
                return;
            }

            _ = _capturingStoppedCompletionSource.TrySetException(new InvalidOperationException(nameof(requestId)));
        }

        public async Task StopAsync(CancellationToken token)
        {
            await _profilerChannel.SendMessage(
                _endpointInfo,
                new JsonProfilerMessage(IpcCommand.StopCapturingParameters, new StopCapturingParametersPayload()
                {
                    RequestId = _requestId
                }),
                token);
        }

        public bool IsStoppable => true;
    }
}
