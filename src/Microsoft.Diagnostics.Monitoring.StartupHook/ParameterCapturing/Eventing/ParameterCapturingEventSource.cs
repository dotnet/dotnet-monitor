// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.StartupHook.Eventing;
using Microsoft.Diagnostics.Tools.Monitor.ParameterCapturing;
using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Reflection;
using static Microsoft.Diagnostics.Tools.Monitor.ParameterCapturing.ParameterCapturingEvents;

namespace Microsoft.Diagnostics.Monitoring.StartupHook.ParameterCapturing.Eventing
{
    [EventSource(Name = ParameterCapturingEvents.SourceName)]
    internal sealed class ParameterCapturingEventSource : AbstractMonitorEventSource
    {
        [Event(ParameterCapturingEvents.EventIds.CapturingStart)]
        public void CapturingStart(Guid RequestId)
        {
            Span<EventData> data = stackalloc EventData[1];

            SetValue(ref data[ParameterCapturingEvents.CapturingActivityPayload.RequestId], RequestId);

            WriteEventWithFlushing(ParameterCapturingEvents.EventIds.CapturingStart, data);
        }

        [Event(ParameterCapturingEvents.EventIds.CapturingStop)]
        public void CapturingStop(Guid RequestId)
        {
            Span<EventData> data = stackalloc EventData[1];

            SetValue(ref data[ParameterCapturingEvents.CapturingActivityPayload.RequestId], RequestId);

            WriteEventWithFlushing(ParameterCapturingEvents.EventIds.CapturingStop, data);
        }

        [Event(ParameterCapturingEvents.EventIds.ServiceStateUpdate)]
        public void ServiceStateUpdate(
            ParameterCapturingEvents.ServiceState State,
            string Details)
        {
            Span<EventData> data = stackalloc EventData[2];
            using PinnedData detailsPinned = PinnedData.Create(Details);

            SetValue(ref data[ParameterCapturingEvents.ServiceStatePayload.State], State);
            SetValue(ref data[ParameterCapturingEvents.ServiceStatePayload.Details], detailsPinned);

            WriteEventWithFlushing(ParameterCapturingEvents.EventIds.ServiceStateUpdate, data);
        }

        [Event(ParameterCapturingEvents.EventIds.UnknownRequestId)]
        public void UnknownRequestId(Guid RequestId)
        {
            Span<EventData> data = stackalloc EventData[1];

            SetValue(ref data[ParameterCapturingEvents.UnknownRequestIdPayload.RequestId], RequestId);

            WriteEventWithFlushing(ParameterCapturingEvents.EventIds.UnknownRequestId, data);
        }

        [NonEvent]
        public void FailedToCapture(Guid RequestId, Exception ex)
        {
            FailedToCapture(RequestId, ParameterCapturingEvents.CapturingFailedReason.InternalError, ex.ToString());
        }

        [Event(ParameterCapturingEvents.EventIds.FailedToCapture)]
        public void FailedToCapture(
            Guid RequestId,
            ParameterCapturingEvents.CapturingFailedReason Reason,
            string Details)
        {
            Span<EventData> data = stackalloc EventData[3];

            using PinnedData detailsPinned = PinnedData.Create(Details);

            SetValue(ref data[ParameterCapturingEvents.CapturingFailedPayloads.RequestId], RequestId);
            SetValue(ref data[ParameterCapturingEvents.CapturingFailedPayloads.Reason], Reason);
            SetValue(ref data[ParameterCapturingEvents.CapturingFailedPayloads.Details], detailsPinned);

            WriteEventWithFlushing(ParameterCapturingEvents.EventIds.FailedToCapture, data);
        }

        [Event(ParameterCapturingEvents.EventIds.ParameterCaptured)]
        public void CapturedParameter(
            Guid RequestId,
            Guid CaptureId,
            string parameterName,
            string parameterType,
            string parameterTypeModuleName,
            string parameterValue,
            ParameterEvaluationResult parameterValueEvaluationResult,
            ParameterAttributes parameterAttributes,
            bool isParameterTypeByRef
            )
        {
            Span<EventData> data = stackalloc EventData[9];

            using PinnedData pinnedName = PinnedData.Create(parameterName);
            using PinnedData pinnedType = PinnedData.Create(parameterType);
            using PinnedData pinnedTypeModuleName = PinnedData.Create(parameterTypeModuleName);
            using PinnedData pinnedValue = PinnedData.Create(parameterValue);

            SetValue(ref data[ParameterCapturingEvents.CapturedParameterPayloads.RequestId], RequestId);
            SetValue(ref data[ParameterCapturingEvents.CapturedParameterPayloads.CaptureId], CaptureId);
            SetValue(ref data[ParameterCapturingEvents.CapturedParameterPayloads.ParameterName], pinnedName);
            SetValue(ref data[ParameterCapturingEvents.CapturedParameterPayloads.ParameterType], pinnedType);
            SetValue(ref data[ParameterCapturingEvents.CapturedParameterPayloads.ParameterTypeModuleName], pinnedTypeModuleName);
            SetValue(ref data[ParameterCapturingEvents.CapturedParameterPayloads.ParameterValue], pinnedValue);
            SetValue(ref data[ParameterCapturingEvents.CapturedParameterPayloads.ParameterValueEvaluationResult], parameterValueEvaluationResult);
            SetValue(ref data[ParameterCapturingEvents.CapturedParameterPayloads.ParameterAttributes], parameterAttributes);
            SetValue(ref data[ParameterCapturingEvents.CapturedParameterPayloads.ParameterTypeIsByRef], isParameterTypeByRef);

            WriteEventWithFlushing(ParameterCapturingEvents.EventIds.ParameterCaptured, data);
        }

        [Event(ParameterCapturingEvents.EventIds.ParametersCapturedStart)]
        public void CapturedParameterStart(
            Guid RequestId,
            Guid CaptureId,
            string activityId,
            ActivityIdFormat activityIdFormat,
            int managedThreadId,
            string methodName,
            string methodModuleName,
            string methodDeclaringTypeName
            )
        {
            Span<EventData> data = stackalloc EventData[8];

            using PinnedData pinnedActivityId = PinnedData.Create(activityId);
            using PinnedData pinnedMethodName = PinnedData.Create(methodName);
            using PinnedData pinnedMethodModuleName = PinnedData.Create(methodModuleName);
            using PinnedData pinnedMethodDeclaringTypeName = PinnedData.Create(methodDeclaringTypeName);

            SetValue(ref data[ParameterCapturingEvents.CapturedParametersStartPayloads.RequestId], RequestId);
            SetValue(ref data[ParameterCapturingEvents.CapturedParametersStartPayloads.CaptureId], CaptureId);
            SetValue(ref data[ParameterCapturingEvents.CapturedParametersStartPayloads.ActivityId], pinnedActivityId);
            SetValue(ref data[ParameterCapturingEvents.CapturedParametersStartPayloads.ActivityIdFormat], activityIdFormat);
            SetValue(ref data[ParameterCapturingEvents.CapturedParametersStartPayloads.ThreadId], managedThreadId);
            SetValue(ref data[ParameterCapturingEvents.CapturedParametersStartPayloads.MethodName], pinnedMethodName);
            SetValue(ref data[ParameterCapturingEvents.CapturedParametersStartPayloads.MethodModuleName], pinnedMethodModuleName);
            SetValue(ref data[ParameterCapturingEvents.CapturedParametersStartPayloads.MethodDeclaringTypeName], pinnedMethodDeclaringTypeName);

            WriteEventWithFlushing(ParameterCapturingEvents.EventIds.ParametersCapturedStart, data);
        }

        [Event(ParameterCapturingEvents.EventIds.ParametersCapturedStop)]
        public void CapturedParameterStop(
            Guid RequestId,
            Guid CaptureId
            )
        {
            Span<EventData> data = stackalloc EventData[2];

            SetValue(ref data[ParameterCapturingEvents.CapturedParametersStopPayloads.RequestId], RequestId);
            SetValue(ref data[ParameterCapturingEvents.CapturedParametersStopPayloads.CaptureId], CaptureId);

            WriteEventWithFlushing(ParameterCapturingEvents.EventIds.ParametersCapturedStop, data);
        }

        [Event(ParameterCapturingEvents.EventIds.Flush)]
        protected override void Flush()
        {
            WriteEvent(ParameterCapturingEvents.EventIds.Flush);
        }
    }
}
