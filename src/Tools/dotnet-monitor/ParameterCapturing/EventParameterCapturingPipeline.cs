// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;
using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.ParameterCapturing
{
    internal sealed class CapturingFailedArgs
    {
        public Guid RequestId { get; set; }
        public ParameterCapturingEvents.CapturingFailedReason Reason { get; set; }
        public string Details { get; set; }
    }

    internal sealed class ServiceNotAvailableArgs
    {
        public ParameterCapturingEvents.ServiceState ServiceState { get; set; }
        public string Details { get; set; }
    }

    internal sealed class EventParameterCapturingPipeline : EventSourcePipeline<EventParameterCapturingPipelineSettings>
    {
        public EventHandler<Guid> OnStartedCapturing;
        public EventHandler<Guid> OnStoppedCapturing;
        public EventHandler<Guid> OnUnknownRequestId;

        public EventHandler<CapturingFailedArgs> OnCapturingFailed;
        public EventHandler<ServiceNotAvailableArgs> OnServiceNotAvailable;

        public EventParameterCapturingPipeline(IpcEndpoint endpoint, EventParameterCapturingPipelineSettings settings)
            : base(new DiagnosticsClient(endpoint), settings)

        {
        }

        protected override MonitoringSourceConfiguration CreateConfiguration()
        {
            return new EventPipeProviderSourceConfiguration(requestRundown: false, bufferSizeInMB: 64, new[]
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
            Console.WriteLine(traceEvent.EventName);
            switch (traceEvent.EventName)
            {
                case "Capturing/Start":
                {
                    byte[] requestIdBytes = traceEvent.GetPayload<byte[]>(ParameterCapturingEvents.CapturingActivityPayload.RequestId);
                    OnStartedCapturing.Invoke(this, new Guid(requestIdBytes));
                    break;
                }
                case "Capturing/Stop":
                {
                    byte[] requestIdBytes = traceEvent.GetPayload<byte[]>(ParameterCapturingEvents.CapturingActivityPayload.RequestId);
                    OnStoppedCapturing.Invoke(this, new Guid(requestIdBytes));
                    break;
                }
                case "UnknownRequestId":
                {
                    byte[] requestIdBytes = traceEvent.GetPayload<byte[]>(ParameterCapturingEvents.CapturingActivityPayload.RequestId);
                    OnUnknownRequestId.Invoke(this, new Guid(requestIdBytes));
                    break;
                }
                case "FailedToCapture":
                {
                    byte[] requestIdBytes = traceEvent.GetPayload<byte[]>(ParameterCapturingEvents.CapturingFailedPayloads.RequestId);
                    ParameterCapturingEvents.CapturingFailedReason reason = traceEvent.GetPayload<ParameterCapturingEvents.CapturingFailedReason>(ParameterCapturingEvents.CapturingFailedPayloads.Reason);
                    string details = traceEvent.GetPayload<string>(ParameterCapturingEvents.CapturingFailedPayloads.Details);

                    OnCapturingFailed.Invoke(this, new CapturingFailedArgs()
                    {
                        RequestId = new Guid(requestIdBytes),
                        Reason = reason,
                        Details = details
                    });
                    break;
                }
                case "ServiceNotAvailable":
                {
                    ParameterCapturingEvents.ServiceState state = traceEvent.GetPayload<ParameterCapturingEvents.ServiceState>(ParameterCapturingEvents.ServiceStatePayload.State);
                    string details = traceEvent.GetPayload<string>(ParameterCapturingEvents.ServiceStatePayload.Details);
                    OnServiceNotAvailable.Invoke(this, new ServiceNotAvailableArgs()
                    {
                        ServiceState = state,
                        Details = details
                    });
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
        public EventParameterCapturingPipelineSettings()
        {
            Duration = Timeout.InfiniteTimeSpan;
        }
    }
}
