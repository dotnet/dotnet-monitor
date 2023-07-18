﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Extensions.Logging;
using System;
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

        public CaptureParametersOperation(IEndpointInfo endpointInfo, ProfilerChannel profilerChannel, ILogger logger, MethodDescription[] methods, TimeSpan duration)
        {
            _profilerChannel = profilerChannel;
            _endpointInfo = endpointInfo;
            _logger = logger;
            _methods = methods;
            _duration = duration;
        }

        public async Task ExecuteAsync(TaskCompletionSource<object> startCompletionSource, CancellationToken token)
        {
            var settings = new EventParameterCapturingPipelineSettings
            {
                Duration = Timeout.InfiniteTimeSpan
            };


            TaskCompletionSource<object> capturingStoppedCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
            TaskCompletionSource<object> capturingStartedCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

            await using EventParameterCapturingPipeline eventTracePipeline = new(_endpointInfo.Endpoint, settings);
            eventTracePipeline.OnStartedCapturing += (_, _) =>
            {
                _ = capturingStartedCompletionSource.TrySetResult(null);
            };
            eventTracePipeline.OnStoppedCapturing += (_, _) =>
            {
                _ = capturingStoppedCompletionSource.TrySetResult(null);
            };
            eventTracePipeline.OnCapturingFailed += (_, failureArgs) =>
            {
                Exception ex;
                switch (failureArgs.Reason)
                {
                    case ParameterCapturingEvents.CapturingFailedReason.UnresolvedMethods:
                    case ParameterCapturingEvents.CapturingFailedReason.InvalidRequest:
                        ex = new MonitoringException(failureArgs.Details);
                        break;
                    default:
                        ex = new InvalidOperationException(failureArgs.Details);
                        break;
                }

                _ = capturingStartedCompletionSource.TrySetException(ex);
            };
            eventTracePipeline.OnServiceNotAvailable += (_, notAvailableArgs) =>
            {
                Exception ex;
                switch (notAvailableArgs.Reason)
                {
                    case ParameterCapturingEvents.ServiceNotAvailableReason.NotSupported:
                        ex = new MonitoringException(notAvailableArgs.Details);
                        break;
                    default:
                        ex = new InvalidOperationException(notAvailableArgs.Details);
                        break;
                }

                _ = capturingStartedCompletionSource.TrySetException(ex);
                _ = capturingStoppedCompletionSource.TrySetException(ex);
            };


            Task runPipelineTask = eventTracePipeline.StartAsync(token);

            await _profilerChannel.SendMessage(
                _endpointInfo,
                new JsonProfilerMessage(IpcCommand.StartCapturingParameters, new StartCapturingParametersPayload
                {
                    Duration = _duration,
                    Methods = _methods
                }),
                token);

            token.Register(async () =>
            {
                await StopAsync(CancellationToken.None);
            });

            await capturingStartedCompletionSource.Task.WaitAsync(token).ConfigureAwait(false);
            startCompletionSource?.TrySetResult(null);

            await capturingStoppedCompletionSource.Task.WaitAsync(token).ConfigureAwait(false);
        }

        public async Task StopAsync(CancellationToken token)
        {
            await _profilerChannel.SendMessage(
                _endpointInfo,
                new JsonProfilerMessage(IpcCommand.StopCapturingParameters, new EmptyPayload()),
                token);
        }

        public bool IsStoppable => true;
    }
}
