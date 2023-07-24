// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.StartupHook.Eventing;
using Microsoft.Diagnostics.Tools.Monitor.ParameterCapturing;
using System;
using System.Diagnostics.Tracing;

namespace Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing.Eventing
{
    [EventSource(Name = ParameterCapturingEvents.SourceName)]
    internal sealed class ParameterCapturingEventSource : AbstractMonitorEventSource
    {
        [NonEvent]
        public void CapturingStart(Guid RequestId)
        {
            CapturingStart(RequestId.ToByteArray());
        }

        [NonEvent]
        public void CapturingStop(Guid RequestId)
        {
            CapturingStop(RequestId.ToByteArray());
        }

        [Event(ParameterCapturingEvents.EventIds.CapturingStart)]
        private void CapturingStart(byte[] RequestId)
        {
            Span<EventData> data = stackalloc EventData[1];

            Span<byte> requestIdSpan = stackalloc byte[GetArrayDataSize(RequestId)];
            FillArrayData(requestIdSpan, RequestId);

            SetValue(ref data[ParameterCapturingEvents.CapturingActivityPayload.RequestId], requestIdSpan);

            WriteEventWithFlushing(ParameterCapturingEvents.EventIds.CapturingStart, data);
        }

        [Event(ParameterCapturingEvents.EventIds.CapturingStop)]
        private void CapturingStop(byte[] RequestId)
        {
            Span<EventData> data = stackalloc EventData[1];

            Span<byte> requestIdSpan = stackalloc byte[GetArrayDataSize(RequestId)];
            FillArrayData(requestIdSpan, RequestId);

            SetValue(ref data[ParameterCapturingEvents.CapturingActivityPayload.RequestId], requestIdSpan);

            WriteEventWithFlushing(ParameterCapturingEvents.EventIds.CapturingStop, data);
        }

        [NonEvent]
        private void CapturingActivityCore(int eventId, byte[] RequestId)
        {
            Span<EventData> data = stackalloc EventData[1];

            Span<byte> requestIdSpan = stackalloc byte[GetArrayDataSize(RequestId)];
            FillArrayData(requestIdSpan, RequestId);

            SetValue(ref data[ParameterCapturingEvents.CapturingActivityPayload.RequestId], requestIdSpan);

            WriteEventWithFlushing(eventId, data);
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

        [NonEvent]
        public void UnknownRequestId(Guid RequestId)
        {
            UnknownRequestId(RequestId.ToByteArray());
        }

        [Event(ParameterCapturingEvents.EventIds.UnknownRequestId)]
        private void UnknownRequestId(byte[] RequestId)
        {
            Span<EventData> data = stackalloc EventData[1];

            Span<byte> requestIdSpan = stackalloc byte[GetArrayDataSize(RequestId)];
            FillArrayData(requestIdSpan, RequestId);

            SetValue(ref data[ParameterCapturingEvents.UnknownRequestIdPayload.RequestId], requestIdSpan);

            WriteEventWithFlushing(ParameterCapturingEvents.EventIds.UnknownRequestId, data);
        }


        [NonEvent]
        public void FailedToCapture(Guid RequestId, Exception ex)
        {
            ParameterCapturingEvents.CapturingFailedReason reason;
            string details;
            if (ex is UnresolvedMethodsExceptions)
            {
                reason = ParameterCapturingEvents.CapturingFailedReason.UnresolvedMethods;
                details = ex.Message;
            }
            else if (ex is ArgumentException)
            {
                reason = ParameterCapturingEvents.CapturingFailedReason.InvalidRequest;
                details = ex.Message;
            }
            else
            {
                reason = ParameterCapturingEvents.CapturingFailedReason.InternalError;
                details = ex.ToString();
            }

            FailedToCapture(RequestId.ToByteArray(), reason, details);
        }

        [NonEvent]
        public void FailedToCapture(
            Guid RequestId,
            ParameterCapturingEvents.CapturingFailedReason Reason,
            string Details)
        {
            FailedToCapture(RequestId.ToByteArray(), Reason, Details);
        }

        [Event(ParameterCapturingEvents.EventIds.FailedToCapture)]
        private void FailedToCapture(
            byte[] RequestId,
            ParameterCapturingEvents.CapturingFailedReason Reason,
            string Details)
        {
            Span<EventData> data = stackalloc EventData[3];

            Span<byte> requestIdSpan = stackalloc byte[GetArrayDataSize(RequestId)];
            FillArrayData(requestIdSpan, RequestId);

            using PinnedData detailsPinned = PinnedData.Create(Details);

            SetValue(ref data[ParameterCapturingEvents.CapturingFailedPayloads.RequestId], requestIdSpan);
            SetValue(ref data[ParameterCapturingEvents.CapturingFailedPayloads.Reason], Reason);
            SetValue(ref data[ParameterCapturingEvents.CapturingFailedPayloads.Details], detailsPinned);

            WriteEventWithFlushing(ParameterCapturingEvents.EventIds.FailedToCapture, data);
        }

        [Event(ParameterCapturingEvents.EventIds.Flush)]
        protected override void Flush()
        {
            WriteEvent(ParameterCapturingEvents.EventIds.Flush);
        }
    }
}
