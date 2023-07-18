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
        public void CapturingStart()
        {
            WriteEventWithFlushing(ParameterCapturingEvents.EventIds.CapturingStart);
        }

        [Event(ParameterCapturingEvents.EventIds.CapturingStop)]
        public void CapturingStop()
        {
            WriteEventWithFlushing(ParameterCapturingEvents.EventIds.CapturingStop);
        }


        [Event(ParameterCapturingEvents.EventIds.ServiceNotAvailable)]
        public void ServiceNotAvailable(
            ParameterCapturingEvents.ServiceNotAvailableReason Reason,
            string Details)
        {
            Span<EventData> data = stackalloc EventData[2];
            using PinnedData detailsPinned = PinnedData.Create(Details);

            SetValue(ref data[ParameterCapturingEvents.ServiceNotAvailablePayload.Reason], Reason);
            SetValue(ref data[ParameterCapturingEvents.ServiceNotAvailablePayload.Details], detailsPinned);

            WriteEventWithFlushing(ParameterCapturingEvents.EventIds.ServiceNotAvailable, data);
        }

        [NonEvent]
        public void FailedToCapture(Exception ex)
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

            FailedToCapture(reason, details);
        }

        [Event(ParameterCapturingEvents.EventIds.FailedToCapture)]
        public void FailedToCapture(
            ParameterCapturingEvents.CapturingFailedReason Reason,
            string Details)
        {
            Span<EventData> data = stackalloc EventData[2];
            using PinnedData detailsPinned = PinnedData.Create(Details);

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
