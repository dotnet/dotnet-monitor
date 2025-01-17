// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.ParameterCapturing;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;
using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.Diagnostics.Tools.Monitor.ParameterCapturing.ParameterCapturingEvents;
using Models = Microsoft.Diagnostics.Monitoring.WebApi.Models;

namespace Microsoft.Diagnostics.Tools.Monitor.ParameterCapturing
{
    internal sealed class CapturingFailedArgs
    {
        public required Guid RequestId { get; set; }
        public required ParameterCapturingEvents.CapturingFailedReason Reason { get; set; }
        public required string Details { get; set; }
    }

    internal sealed class ServiceStateUpdateArgs
    {
        public required ParameterCapturingEvents.ServiceState ServiceState { get; set; }
        public required string Details { get; set; }
    }

    internal sealed class EventParameterCapturingPipeline : EventSourcePipeline<EventParameterCapturingPipelineSettings>
    {
        private readonly CapturedParametersBuilder _parameterBuilder = new();

        public EventParameterCapturingPipeline(IpcEndpoint endpoint, EventParameterCapturingPipelineSettings settings)
            : base(new DiagnosticsClient(endpoint), settings)

        {
        }

        protected override MonitoringSourceConfiguration CreateConfiguration()
        {
            return new EventPipeProviderSourceConfiguration(rundownKeyword: 0, bufferSizeInMB: 64, new[]
            {
                new EventPipeProvider(ParameterCapturingEvents.SourceName, EventLevel.Informational, (long)EventKeywords.All)
            });
        }

        protected override Task OnEventSourceAvailable(EventPipeEventSource eventSource, Func<Task> stopSessionAsync, CancellationToken token)
        {
            eventSource.Dynamic.AddCallbackForProviderEvent(
                ParameterCapturingEvents.SourceName,
                null,
                Callback);

            return Task.CompletedTask;
        }

        private void Callback(TraceEvent traceEvent)
        {
            switch (traceEvent.EventName)
            {
                case "Capturing/Start":
                    {
                        Guid requestId = traceEvent.GetPayload<Guid>(ParameterCapturingEvents.CapturingActivityPayload.RequestId);
                        Settings.OnStartedCapturing?.Invoke(this, requestId);
                        break;
                    }
                case "Capturing/Stop":
                    {
                        Guid requestId = traceEvent.GetPayload<Guid>(ParameterCapturingEvents.CapturingActivityPayload.RequestId);
                        Settings.OnStoppedCapturing?.Invoke(this, requestId);
                        break;
                    }
                case "UnknownRequestId":
                    {
                        Guid requestId = traceEvent.GetPayload<Guid>(ParameterCapturingEvents.UnknownRequestIdPayload.RequestId);
                        Settings.OnUnknownRequestId?.Invoke(this, requestId);
                        break;
                    }
                case "FailedToCapture":
                    {
                        Guid requestId = traceEvent.GetPayload<Guid>(ParameterCapturingEvents.CapturingFailedPayloads.RequestId);
                        ParameterCapturingEvents.CapturingFailedReason reason = traceEvent.GetPayload<ParameterCapturingEvents.CapturingFailedReason>(ParameterCapturingEvents.CapturingFailedPayloads.Reason);
                        string details = traceEvent.GetPayload<string>(ParameterCapturingEvents.CapturingFailedPayloads.Details);

                        Settings.OnCapturingFailed?.Invoke(this, new CapturingFailedArgs()
                        {
                            RequestId = requestId,
                            Reason = reason,
                            Details = details
                        });
                        break;
                    }
                case "ServiceStateUpdate":
                    {
                        ParameterCapturingEvents.ServiceState state = traceEvent.GetPayload<ParameterCapturingEvents.ServiceState>(ParameterCapturingEvents.ServiceStatePayload.State);
                        string details = traceEvent.GetPayload<string>(ParameterCapturingEvents.ServiceStatePayload.Details);
                        Settings.OnServiceStateUpdate?.Invoke(this, new ServiceStateUpdateArgs()
                        {
                            ServiceState = state,
                            Details = details
                        });
                        break;
                    }
                case "CapturedParameter/Start":
                    {
                        Guid requestId = traceEvent.GetPayload<Guid>(ParameterCapturingEvents.CapturedParametersStartPayloads.RequestId);
                        Guid captureId = traceEvent.GetPayload<Guid>(ParameterCapturingEvents.CapturedParametersStartPayloads.CaptureId);
                        string activityId = traceEvent.GetPayload<string>(ParameterCapturingEvents.CapturedParametersStartPayloads.ActivityId);
                        ActivityIdFormat activityIdFormat = traceEvent.GetPayload<ActivityIdFormat>(ParameterCapturingEvents.CapturedParametersStartPayloads.ActivityIdFormat);
                        int threadId = traceEvent.GetPayload<int>(ParameterCapturingEvents.CapturedParametersStartPayloads.ThreadId);
                        string methodName = traceEvent.GetPayload<string>(ParameterCapturingEvents.CapturedParametersStartPayloads.MethodName);
                        string methodModuleName = traceEvent.GetPayload<string>(ParameterCapturingEvents.CapturedParametersStartPayloads.MethodModuleName);
                        string methodDeclaringTypeName = traceEvent.GetPayload<string>(ParameterCapturingEvents.CapturedParametersStartPayloads.MethodDeclaringTypeName);

                        _ = _parameterBuilder.TryStartNewCaptureResponse(
                                captureId,
                                string.IsNullOrEmpty(activityId) ? null : activityId,
                                activityIdFormat,
                                threadId,
                                traceEvent.TimeStamp,
                                methodName: methodName,
                                methodTypeName: methodDeclaringTypeName,
                                methodModuleName: methodModuleName);

                        break;
                    }
                case "CapturedParameter":
                    {
                        Guid requestId = traceEvent.GetPayload<Guid>(ParameterCapturingEvents.CapturedParameterPayloads.RequestId);
                        Guid captureId = traceEvent.GetPayload<Guid>(ParameterCapturingEvents.CapturedParameterPayloads.CaptureId);
                        string parameterName = traceEvent.GetPayload<string>(ParameterCapturingEvents.CapturedParameterPayloads.ParameterName);
                        string parameterType = traceEvent.GetPayload<string>(ParameterCapturingEvents.CapturedParameterPayloads.ParameterType);
                        string parameterTypeModuleName = traceEvent.GetPayload<string>(ParameterCapturingEvents.CapturedParameterPayloads.ParameterTypeModuleName);
                        string parameterValue = traceEvent.GetPayload<string>(ParameterCapturingEvents.CapturedParameterPayloads.ParameterValue);
                        ParameterEvaluationResult parameterValueEvaluationResult = traceEvent.GetPayload<ParameterEvaluationResult>(ParameterCapturingEvents.CapturedParameterPayloads.ParameterValueEvaluationResult);
                        ParameterAttributes parameterAttributes = traceEvent.GetPayload<ParameterAttributes>(ParameterCapturingEvents.CapturedParameterPayloads.ParameterAttributes);
                        bool isByRefParameter = traceEvent.GetPayload<bool>(ParameterCapturingEvents.CapturedParameterPayloads.ParameterTypeIsByRef);

                        Models.EvaluationFailureReason evalFailReason = parameterValueEvaluationResult switch
                        {
                            ParameterEvaluationResult.UnsupportedEval => Models.EvaluationFailureReason.NotSupported,
                            ParameterEvaluationResult.EvalHasSideEffects => Models.EvaluationFailureReason.HasSideEffects,
                            ParameterEvaluationResult.FailedEval => Models.EvaluationFailureReason.Unknown,
                            _ => Models.EvaluationFailureReason.None,
                        };

                        _ = _parameterBuilder.TryAddParameter(
                            captureId: captureId,
                            parameterName: parameterName,
                            parameterType: parameterType,
                            parameterTypeModuleName: parameterTypeModuleName,
                            parameterValue: parameterValueEvaluationResult == ParameterEvaluationResult.IsNull ? null : parameterValue,
                            evalFailReason: evalFailReason,
                            isInParameter: (parameterAttributes & ParameterAttributes.In) != 0,
                            isOutParameter: (parameterAttributes & ParameterAttributes.Out) != 0,
                            isByRefParameter: isByRefParameter);

                        break;
                    }
                case "CapturedParameter/Stop":
                    {
                        Guid requestId = traceEvent.GetPayload<Guid>(ParameterCapturingEvents.CapturedParametersStopPayloads.RequestId);
                        Guid captureId = traceEvent.GetPayload<Guid>(ParameterCapturingEvents.CapturedParametersStopPayloads.CaptureId);

                        if (_parameterBuilder.TryFinalizeParameters(captureId, out ICapturedParameters? capturedParameters))
                        {
                            Settings.OnParametersCaptured?.Invoke(this, capturedParameters);
                        }
                        break;
                    }
                case "Flush":
                    break;
#if DEBUG
                default:
                    throw new NotSupportedException("Unhandled event: " + traceEvent.EventName);
#endif
            }
        }

        public new Task StartAsync(CancellationToken token)
        {
            return base.StartAsync(token);
        }
    }

    internal sealed class EventParameterCapturingPipelineSettings : EventSourcePipelineSettings
    {
        public EventHandler<Guid>? OnStartedCapturing;
        public EventHandler<Guid>? OnStoppedCapturing;
        public EventHandler<Guid>? OnUnknownRequestId;

        public EventHandler<CapturingFailedArgs>? OnCapturingFailed;
        public EventHandler<ServiceStateUpdateArgs>? OnServiceStateUpdate;

        public EventHandler<ICapturedParameters>? OnParametersCaptured;

        public EventParameterCapturingPipelineSettings()
        {
            Duration = Timeout.InfiniteTimeSpan;
        }
    }
}
