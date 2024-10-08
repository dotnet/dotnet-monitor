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
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.ParameterCapturing
{
    internal sealed class CaptureParametersOperation : IInProcessOperation
    {
        private readonly ProfilerChannel _profilerChannel;
        private readonly IEndpointInfo _endpointInfo;
        private readonly ILogger _logger;
        private readonly CaptureParametersConfiguration _configuration;
        private readonly TimeSpan _duration;

        private readonly Guid _requestId;

        private readonly TaskCompletionSource _capturingStoppedCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _capturingStartedCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool IsStoppable => true;

        public Task Started => _capturingStartedCompletionSource.Task;

        public CaptureParametersOperation(IEndpointInfo endpointInfo, ProfilerChannel profilerChannel, ILogger logger, CaptureParametersConfiguration configuration, TimeSpan duration)
        {
            _profilerChannel = profilerChannel;
            _endpointInfo = endpointInfo;
            _logger = logger;
            _configuration = configuration;
            _duration = duration;

            _requestId = Guid.NewGuid();
        }

        public static bool IsEndpointRuntimeSupported(IEndpointInfo endpointInfo)
        {
            // net 7+ is required, see https://github.com/dotnet/runtime/issues/88924 for more information
            return endpointInfo.RuntimeVersion != null && endpointInfo.RuntimeVersion.Major >= 7;
        }

        private async Task EnsureEndpointCanProcessRequestsAsync(CancellationToken token)
        {
            static Exception getNotAvailableException(string reason)
            {
                return new MonitoringException(string.Format(
                        CultureInfo.InvariantCulture,
                        Strings.ErrorMessage_ParameterCapturingNotAvailable,
                        reason));
            }

            if (!IsEndpointRuntimeSupported(_endpointInfo))
            {
                throw getNotAvailableException(Strings.ParameterCapturingNotAvailable_Reason_UnsupportedRuntime);
            }

            DiagnosticsClient client = new(_endpointInfo.Endpoint);

            IDictionary<string, string> env = await client.GetProcessEnvironmentAsync(token);

            const string PreventHostingStartupEnvName = "ASPNETCORE_PREVENTHOSTINGSTARTUP";
            if (env.TryGetValue(PreventHostingStartupEnvName, out string preventHostingStartupEnvValue) &&
                ToolIdentifiers.IsEnvVarValueEnabled(preventHostingStartupEnvValue))
            {
                throw getNotAvailableException(Strings.ParameterCapturingNotAvailable_Reason_PreventedHostingStartup);
            }

            if (!env.TryGetValue(InProcessFeaturesIdentifiers.EnvironmentVariables.AvailableInfrastructure.ManagedMessaging, out string isManagedMessagingAvailable) ||
                !ToolIdentifiers.IsEnvVarValueEnabled(isManagedMessagingAvailable))
            {
                throw getNotAvailableException(Strings.ParameterCapturingNotAvailable_Reason_ManagedMessagingDidNotLoad);
            }

            if (!env.TryGetValue(InProcessFeaturesIdentifiers.EnvironmentVariables.AvailableInfrastructure.HostingStartup, out string isHostingStartupAvailable) ||
                !ToolIdentifiers.IsEnvVarValueEnabled(isHostingStartupAvailable))
            {
                throw getNotAvailableException(Strings.ParameterCapturingNotAvailable_Reason_HostingStartupDidNotLoad);
            }

            if ((env.TryGetValue("DOTNET_ForceEnc", out string editAndContinueEnvValue) || env.TryGetValue("COMPlus_ForceEnc", out editAndContinueEnvValue)) &&
                ToolIdentifiers.IsEnvVarValueEnabled(editAndContinueEnvValue))
            {
                // Having Enc enabled results in methods belonging to debug modules to silently fail being instrumented.
                // ref: https://github.com/dotnet/runtime/issues/91963
                throw getNotAvailableException(Strings.ParameterCapturingNotAvailable_Reason_HotReload);
            }
        }

        public async Task ExecuteAsync(CancellationToken token)
        {
            try
            {
                // Check if the endpoint is capable of responding to our requests
                await EnsureEndpointCanProcessRequestsAsync(token);

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
                        Configuration = _configuration
                    }),
                    token);

                await _capturingStartedCompletionSource.Task.WaitAsync(token).ConfigureAwait(false);
                await _capturingStoppedCompletionSource.Task.WaitAsync(token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                _ = _capturingStartedCompletionSource.TrySetCanceled(token);
                _ = _capturingStoppedCompletionSource.TrySetCanceled(token);

                using CancellationTokenSource stopCancellationToken = new(TimeSpan.FromSeconds(30));
                await StopAsync(stopCancellationToken.Token).SafeAwait();
                throw;
            }
            catch (Exception ex)
            {
                _ = _capturingStartedCompletionSource.TrySetException(ex);
                _ = _capturingStoppedCompletionSource.TrySetException(ex);
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
    }
}
