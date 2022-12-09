// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.EventPipe.Triggers;
using Microsoft.Diagnostics.Monitoring.EventPipe.Triggers.Pipelines;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.NETCore.Client;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Triggers
{
    internal sealed class EventPipeTriggerFactory
    {
        /// <summary>
        /// Creates a collection rule trigger sourced from the event pipe of the target
        /// process represented by the specified endpoint.
        /// </summary>
        public static ICollectionRuleTrigger Create<TSettings>(
            IEndpointInfo endpointInfo,
            MonitoringSourceConfiguration configuration,
            ITraceEventTriggerFactory<TSettings> factory,
            TSettings settings,
            Action callback)
        {
            return new EventPipeTrigger<TSettings>(
                endpointInfo,
                configuration,
                factory,
                settings,
                callback);
        }

        private sealed class EventPipeTrigger<TSettings> :
            ICollectionRuleTrigger,
            IAsyncDisposable
        {
            private readonly EventPipeTriggerPipeline<TSettings> _pipeline;

            public EventPipeTrigger(
                IEndpointInfo endpointInfo,
                MonitoringSourceConfiguration configuration,
                ITraceEventTriggerFactory<TSettings> factory,
                TSettings settings,
                Action callback)
            {
                EventPipeTriggerPipelineSettings<TSettings> pipelineSettings = new()
                {
                    Configuration = configuration,
                    Duration = Timeout.InfiniteTimeSpan,
                    TriggerFactory = factory,
                    TriggerSettings = settings
                };

                _pipeline = new EventPipeTriggerPipeline<TSettings>(
                    new DiagnosticsClient(endpointInfo.Endpoint),
                    pipelineSettings,
                    _ => callback());
            }

            public async Task StartAsync(CancellationToken cancellationToken)
            {
                // Wrap the passed CancellationToken into a linked CancellationTokenSource so that the
                // RunAsync method is only cancellable for the execution of the StartAsync method. Don't
                // want the caller to be able to cancel the run of the pipeline after having finished
                // executing the StartAsync method.
                using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                await _pipeline.StartAsync(cts.Token);
            }

            public Task StopAsync(CancellationToken cancellationToken)
            {
                return _pipeline.StopAsync(cancellationToken);
            }

            public async ValueTask DisposeAsync()
            {
                await _pipeline.DisposeAsync();
            }
        }
    }
}
