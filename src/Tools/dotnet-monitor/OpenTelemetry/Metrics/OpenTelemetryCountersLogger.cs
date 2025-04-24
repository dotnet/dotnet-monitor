// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if BUILDING_OTEL
#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Configuration;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

namespace Microsoft.Diagnostics.Tools.Monitor.OpenTelemetry.Metrics;

internal sealed class OpenTelemetryCountersLogger : ICountersLogger
{
    private readonly ILoggerFactory _LoggerFactory;
    private readonly Resource _Resource;
    private readonly OpenTelemetryOptions _Options;
    private readonly MetricsStore _MetricsStore;

    private IMetricReader? _MetricReader;

    public OpenTelemetryCountersLogger(
        ILoggerFactory loggerFactory,
        Resource resource,
        OpenTelemetryOptions options)
    {
        _LoggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _Resource = resource ?? throw new ArgumentNullException(nameof(resource));
        _Options = options ?? throw new ArgumentNullException(nameof(options));

        if (_Options.ExporterOptions.ExporterType != "OpenTelemetryProtocol"
            || _Options.ExporterOptions.OpenTelemetryProtocolExporterOptions == null)
        {
            throw new InvalidOperationException("Options were invalid.");
        }

        _MetricsStore = new MetricsStore(
            loggerFactory.CreateLogger<MetricsStoreService>(),
            maxMetricCount: int.MaxValue);
    }

    public Task PipelineStarted(CancellationToken token)
    {
        _MetricReader ??= OpenTelemetryFactory.CreatePeriodicExportingMetricReaderAsync(
            _LoggerFactory,
            _Resource,
            _Options.ExporterOptions,
            _Options.MetricsOptions.PeriodicExportingOptions,
            [new OpenTelemetryMetricProducerFactory(_MetricsStore)]);

        return Task.CompletedTask;
    }

    public async Task PipelineStopped(CancellationToken token)
    {
        var metricReader = _MetricReader;
        if (metricReader != null)
        {
            await metricReader.ShutdownAsync(token);
            metricReader.Dispose();
            _MetricReader = null;
            _MetricsStore.Clear();
        }
    }

    public void Log(ICounterPayload counter)
    {
        if (counter.IsMeter)
        {
            _MetricsStore.AddMetric(counter);
        }
    }
}
#endif
