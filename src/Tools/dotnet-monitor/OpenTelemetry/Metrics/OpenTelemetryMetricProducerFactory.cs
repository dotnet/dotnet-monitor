// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if BUILDING_OTEL
#nullable enable

using System.Diagnostics;
using Microsoft.Diagnostics.Monitoring.WebApi;
using OpenTelemetry.Metrics;

namespace Microsoft.Diagnostics.Tools.Monitor.OpenTelemetry.Metrics;

internal sealed class OpenTelemetryMetricProducerFactory : IMetricProducerFactory
{
    private readonly MetricsStore _MetricsStore;

    public OpenTelemetryMetricProducerFactory(
        MetricsStore metricsStore)
    {
        Debug.Assert(metricsStore != null);

        _MetricsStore = metricsStore;
    }

    public MetricProducer Create(MetricProducerOptions options)
        => new OpenTelemetryMetricProducer(_MetricsStore, options.AggregationTemporality);
}
#endif
