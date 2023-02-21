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
        private readonly EventExceptionsPipelineNameCache _cache = new();
        private readonly IExceptionsStore _store;

        public EventExceptionsPipeline(IpcEndpoint endpoint, EventExceptionsPipelineSettings settings, IExceptionsStore store)
            : this(new DiagnosticsClient(endpoint), settings, store)
        {
        }

        public EventExceptionsPipeline(DiagnosticsClient client, EventExceptionsPipelineSettings settings, IExceptionsStore store)
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
            // Using event name instead of event ID because event ID seem to be dynamically assigned
            // in the order in which they are used.
            switch (traceEvent.EventName)
            {
                case "ClassDescription":
                    _cache.AddClass(
                        traceEvent.GetPayload<ulong>(NameIdentificationEvents.ClassDescPayloads.ClassId),
                        traceEvent.GetPayload<uint>(NameIdentificationEvents.ClassDescPayloads.Token),
                        traceEvent.GetPayload<ulong>(NameIdentificationEvents.ClassDescPayloads.ModuleId),
                        traceEvent.GetPayload<ClassFlags>(NameIdentificationEvents.ClassDescPayloads.Flags),
                        traceEvent.GetPayload<ulong[]>(NameIdentificationEvents.ClassDescPayloads.TypeArgs)
                        );
                    break;
                case "ExceptionIdentifier":
                    _cache.AddExceptionIdentifier(
                        traceEvent.GetPayload<ulong>(ExceptionEvents.ExceptionIdentifierPayloads.ExceptionId),
                        traceEvent.GetPayload<ulong>(ExceptionEvents.ExceptionIdentifierPayloads.ExceptionClassId),
                        traceEvent.GetPayload<ulong>(ExceptionEvents.ExceptionIdentifierPayloads.ThrowingMethodId),
                        traceEvent.GetPayload<int>(ExceptionEvents.ExceptionIdentifierPayloads.ILOffset)
                        );
                    break;
                case "ExceptionInstance":
                    ulong exceptionId = traceEvent.GetPayload<ulong>(ExceptionEvents.ExceptionInstancePayloads.ExceptionId);
                    string message = traceEvent.GetPayload<string>(ExceptionEvents.ExceptionInstancePayloads.ExceptionMessage);
                    // Add data to cache and write directly to store; this allows the pipeline to recreate the cache without
                    // affecting the store so long as the cache is not cleared. Example of this may be that the event source
                    // wants to reset the identifiers so as to not indefinitely grow the cache and have a large memory impact.
                    _cache.AddExceptionInstance(exceptionId, message);
                    _store.AddExceptionInstance(_cache, exceptionId, message);
                    break;
                case "FunctionDescription":
                    _cache.AddFunction(
                        traceEvent.GetPayload<ulong>(NameIdentificationEvents.FunctionDescPayloads.FunctionId),
                        traceEvent.GetPayload<ulong>(NameIdentificationEvents.FunctionDescPayloads.ClassId),
                        traceEvent.GetPayload<uint>(NameIdentificationEvents.FunctionDescPayloads.ClassToken),
                        traceEvent.GetPayload<ulong>(NameIdentificationEvents.FunctionDescPayloads.ModuleId),
                        traceEvent.GetPayload<string>(NameIdentificationEvents.FunctionDescPayloads.Name),
                        traceEvent.GetPayload<ulong[]>(NameIdentificationEvents.FunctionDescPayloads.TypeArgs)
                        );
                    break;
                case "ModuleDescription":
                    _cache.AddModule(
                        traceEvent.GetPayload<ulong>(NameIdentificationEvents.ModuleDescPayloads.ModuleId),
                        traceEvent.GetPayload<string>(NameIdentificationEvents.ModuleDescPayloads.Name)
                        );
                    break;
                case "TokenDescription":
                    _cache.AddToken(
                        traceEvent.GetPayload<ulong>(NameIdentificationEvents.TokenDescPayloads.ModuleId),
                        traceEvent.GetPayload<uint>(NameIdentificationEvents.TokenDescPayloads.Token),
                        traceEvent.GetPayload<uint>(NameIdentificationEvents.TokenDescPayloads.OuterToken),
                        traceEvent.GetPayload<string>(NameIdentificationEvents.TokenDescPayloads.Name)
                        );
                    break;
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

    internal sealed class EventExceptionsPipelineSettings : EventSourcePipelineSettings
    {
        public EventExceptionsPipelineSettings()
        {
            Duration = Timeout.InfiniteTimeSpan;
        }
    }
}
