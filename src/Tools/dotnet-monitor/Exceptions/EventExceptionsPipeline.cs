// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Exceptions;
using Microsoft.Diagnostics.Monitoring.WebApi.Stacks;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;
using System;
using System.Diagnostics.Tracing;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.Exceptions
{
    internal sealed class EventExceptionsPipeline : EventSourcePipeline<EventExceptionsPipelineSettings>
    {
        private readonly ExceptionsStore _store;

        public EventExceptionsPipeline(IpcEndpoint endpoint, EventExceptionsPipelineSettings settings, ExceptionsStore store)
            : this(new DiagnosticsClient(endpoint), settings, store)
        {
        }

        public EventExceptionsPipeline(DiagnosticsClient client, EventExceptionsPipelineSettings settings, ExceptionsStore store)
            : base(client, settings)
        {
            ArgumentNullException.ThrowIfNull(store, nameof(store));

            _store = store;
        }

        protected override MonitoringSourceConfiguration CreateConfiguration()
        {
            return new EventPipeProviderSourceConfiguration(requestRundown: false, bufferSizeInMB: 64, new[]
            {
                new EventPipeProvider(ExceptionEvents.SourceName, EventLevel.Informational, (long)EventKeywords.All)
            });
        }

        protected override Task OnEventSourceAvailable(EventPipeEventSource eventSource, Func<Task> stopSessionAsync, CancellationToken token)
        {
            eventSource.Dynamic.AddCallbackForProviderEvent(
                ExceptionEvents.SourceName,
                null,
                Callback);

            return Task.CompletedTask;
        }

        private void Callback(TraceEvent traceEvent)
        {
            switch (traceEvent.EventName)
            {
                case "ClassDescription":
                    _store.AddClass(
                        traceEvent.GetPayload<ulong>(NameIdentificationEvents.ClassDescPayloads.ClassId),
                        traceEvent.GetPayload<uint>(NameIdentificationEvents.ClassDescPayloads.Token),
                        traceEvent.GetPayload<ulong>(NameIdentificationEvents.ClassDescPayloads.ModuleId),
                        traceEvent.GetPayload<ClassFlags>(NameIdentificationEvents.ClassDescPayloads.Flags),
                        traceEvent.GetPayload<ulong[]>(NameIdentificationEvents.ClassDescPayloads.TypeArgs)
                        );
                    break;
                case "ExceptionIdentifier":
                    _store.AddExceptionIdentifier(
                        traceEvent.GetPayload<ulong>(ExceptionEvents.ExceptionIdentifierPayloads.ExceptionId),
                        traceEvent.GetPayload<ulong>(ExceptionEvents.ExceptionIdentifierPayloads.ExceptionClassId),
                        traceEvent.GetPayload<ulong>(ExceptionEvents.ExceptionIdentifierPayloads.ThrowingMethodId),
                        traceEvent.GetPayload<int>(ExceptionEvents.ExceptionIdentifierPayloads.ILOffset)
                        );
                    break;
                case "ExceptionInstance":
                    _store.AddExceptionInstance(
                        traceEvent.GetPayload<ulong>(ExceptionEvents.ExceptionInstancePayloads.ExceptionId),
                        traceEvent.GetPayload<string>(ExceptionEvents.ExceptionInstancePayloads.ExceptionMessage)
                        );
                    break;
                case "FunctionDescription":
                    _store.AddFunction(
                        traceEvent.GetPayload<ulong>(NameIdentificationEvents.FunctionDescPayloads.FunctionId),
                        traceEvent.GetPayload<ulong>(NameIdentificationEvents.FunctionDescPayloads.ClassId),
                        traceEvent.GetPayload<uint>(NameIdentificationEvents.FunctionDescPayloads.ClassToken),
                        traceEvent.GetPayload<ulong>(NameIdentificationEvents.FunctionDescPayloads.ModuleId),
                        traceEvent.GetPayload<string>(NameIdentificationEvents.FunctionDescPayloads.Name),
                        traceEvent.GetPayload<ulong[]>(NameIdentificationEvents.FunctionDescPayloads.TypeArgs)
                        );
                    break;
                case "ModuleDescription":
                    _store.AddModule(
                        traceEvent.GetPayload<ulong>(NameIdentificationEvents.ModuleDescPayloads.ModuleId),
                        traceEvent.GetPayload<string>(NameIdentificationEvents.ModuleDescPayloads.Name)
                        );
                    break;
                case "TokenDescription":
                    _store.AddToken(
                        traceEvent.GetPayload<ulong>(NameIdentificationEvents.TokenDescPayloads.ModuleId),
                        traceEvent.GetPayload<uint>(NameIdentificationEvents.TokenDescPayloads.Token),
                        traceEvent.GetPayload<uint>(NameIdentificationEvents.TokenDescPayloads.OuterToken),
                        traceEvent.GetPayload<string>(NameIdentificationEvents.TokenDescPayloads.Name)
                        );
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

    internal sealed class EventExceptionsPipelineSettings : EventSourcePipelineSettings
    {
        public EventExceptionsPipelineSettings()
        {
            Duration = Timeout.InfiniteTimeSpan;
        }
    }
}
