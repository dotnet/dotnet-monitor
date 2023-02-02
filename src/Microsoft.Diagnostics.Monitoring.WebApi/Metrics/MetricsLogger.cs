// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal sealed class MetricsLogger : ICountersLogger
    {
        private readonly IMetricsStore _store;

        public MetricsLogger(IMetricsStore metricsStore)
        {
            _store = metricsStore;
        }

        public void Log(ICounterPayload metric)
        {
            _store.AddMetric(metric);
        }

        public Task PipelineStarted(CancellationToken token) => Task.CompletedTask;

        public Task PipelineStopped(CancellationToken token) => Task.CompletedTask;
    }
}
