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

        [Event(ParameterCapturingEvents.EventIds.Flush)]
        protected override void Flush()
        {
            WriteEvent(ParameterCapturingEvents.EventIds.Flush);
        }
    }
}
