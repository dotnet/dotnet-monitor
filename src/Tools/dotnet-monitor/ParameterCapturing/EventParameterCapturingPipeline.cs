// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;
using System;
using System.Diagnostics.Tracing;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.ParameterCapturing
{
    internal sealed class CapturingFailedArgs
    {
        public ParameterCapturingEvents.CapturingFailedReason Reason { get; set; }
        public string Details { get; set; }
    }

    internal sealed class ServiceNotAvailableArgs
    {
        public ParameterCapturingEvents.ServiceNotAvailableReason Reason { get; set; }
        public string Details { get; set; }
    }

    internal sealed class EventParameterCapturingPipeline : EventSourcePipeline<EventParameterCapturingPipelineSettings>
    {
        public EventHandler OnStartedCapturing;
        public EventHandler OnStoppedCapturing;

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
            switch (traceEvent.EventName)
            {
                case "Capturing/Start":
                    OnStartedCapturing.Invoke(this, EventArgs.Empty);
                    break;
                case "Capturing/Stop":
                    OnStoppedCapturing.Invoke(this, EventArgs.Empty);
                    break;
                case "FailedToCapture":
                {
                    ParameterCapturingEvents.CapturingFailedReason reason = traceEvent.GetPayload<ParameterCapturingEvents.CapturingFailedReason>(ParameterCapturingEvents.CapturingFailedPayloads.Reason);
                    string details = traceEvent.GetPayload<string>(ParameterCapturingEvents.CapturingFailedPayloads.Details);

                    OnCapturingFailed.Invoke(this, new CapturingFailedArgs()
                    {
                        Reason = reason,
                        Details = details
                    });
                    break;
                }
                case "ServiceNotAvailable":
                {
                    ParameterCapturingEvents.ServiceNotAvailableReason reason = traceEvent.GetPayload<ParameterCapturingEvents.ServiceNotAvailableReason>(ParameterCapturingEvents.ServiceNotAvailablePayload.Reason);
                    string details = traceEvent.GetPayload<string>(ParameterCapturingEvents.ServiceNotAvailablePayload.Details);
                    OnServiceNotAvailable.Invoke(this, new ServiceNotAvailableArgs()
                    {
                        Reason = reason,
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
