// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if BUILDING_OTEL
#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Monitoring.OpenTelemetry;
using Microsoft.Diagnostics.Monitoring.OpenTelemetry.Logging;
using Microsoft.Diagnostics.Monitoring.OpenTelemetry.OpenTelemetryProtocol.Logging;
using Microsoft.Diagnostics.Monitoring.OpenTelemetry.OpenTelemetryProtocol.Metrics;
using Microsoft.Diagnostics.Monitoring.OpenTelemetry.OpenTelemetryProtocol.Tracing;
using Microsoft.Diagnostics.Monitoring.OpenTelemetry.Metrics;
using Microsoft.Diagnostics.Monitoring.OpenTelemetry.Resources;
using Microsoft.Diagnostics.Monitoring.OpenTelemetry.Tracing;
using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Diagnostics.Tools.Monitor.OpenTelemetry;

internal sealed class OpenTelemetryEndpointListener
{
    private readonly ILoggerFactory _LoggerFactory;
    private readonly ILogger<OpenTelemetryEndpointListener> _Logger;
    private readonly IOptionsMonitor<OpenTelemetryOptions> _OpenTelemetryOptions;
    private readonly IEndpointInfo _EndpointInfo;
    private Task? _ProcessTask;
    private CancellationTokenSource? _StoppingToken;

    public OpenTelemetryEndpointListener(
        ILoggerFactory loggerFactory,
        IOptionsMonitor<OpenTelemetryOptions> openTelemetryOptions,
        IEndpointInfo endpointInfo)
    {
        _LoggerFactory = loggerFactory;
        _OpenTelemetryOptions = openTelemetryOptions;
        _EndpointInfo = endpointInfo;

        _Logger = loggerFactory.CreateLogger<OpenTelemetryEndpointListener>();
    }

    public void StartListening()
    {
        if (_StoppingToken == null)
        {
            _StoppingToken = new();
            _ProcessTask = Task.Run(() => Process(_StoppingToken.Token));
        }
    }

    public async void StopListening()
    {
        if (_StoppingToken != null)
        {
            _StoppingToken.Cancel();
            try
            {
                await _ProcessTask!;
            }
            catch (OperationCanceledException)
            {
            }
            _StoppingToken.Dispose();
            _StoppingToken = null;
        }
    }

    private async Task Process(CancellationToken stoppingToken)
    {
        var scopeValue = new KeyValueLogScope();
        scopeValue.AddArtifactEndpointInfo(_EndpointInfo);

        using var scope = _Logger.BeginScope(scopeValue);

        while (!stoppingToken.IsCancellationRequested)
        {
            var options = _OpenTelemetryOptions.Get(name: null);

            var logsEnabled = options.LogsOptions.DefaultLogLevel != null
                || options.LogsOptions.CategoryOptions.Any();
            var metricsEnabled = options.MetricsOptions.MeterOptions.Any();
            var tracingEnabled = options.TracesOptions.Sources.Any();

            if (!logsEnabled && !metricsEnabled && !tracingEnabled)
            {
                _Logger.LogInformation("No OpenTelemetry configuration found process will not be monitored");
                await OpenTelemetryService.WaitForOptionsReloadOrStop(_OpenTelemetryOptions, stoppingToken);
                continue;
            }

            if (options.ExporterOptions.ExporterType != OpenTelemetryExporterType.OpenTelemetryProtocol)
            {
                _Logger.LogInformation("OpenTelemetry configuration ExporterType '{ExporterType}' is not supported", options.ExporterOptions.ExporterType);
                await OpenTelemetryService.WaitForOptionsReloadOrStop(_OpenTelemetryOptions, stoppingToken);
                continue;
            }

            var client = new DiagnosticsClient(_EndpointInfo.Endpoint);

            var resource = await BuildResourceForProcess(_EndpointInfo, client, options, stoppingToken);

            _Logger.LogInformation("Resource created for process with {NumberOfKeys} keys.", resource.Attributes.Count);

            using var optionsTokenSource = new CancellationTokenSource();

            using var _ = _OpenTelemetryOptions.OnChange(o => optionsTokenSource.SafeCancel());

            using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                stoppingToken,
                optionsTokenSource.Token);

            var tasks = new List<Task>();

            if (logsEnabled)
            {
                tasks.Add(
                    ListenToLogs(options, client, resource, linkedTokenSource.Token));
            }

            if (metricsEnabled)
            {
                tasks.Add(
                    ListenToMetrics(options, client, _EndpointInfo.RuntimeVersion, resource, linkedTokenSource.Token));
            }

            if (tracingEnabled)
            {
                tasks.Add(
                    ListenToTraces(options, client, _EndpointInfo.RuntimeVersion, resource, linkedTokenSource.Token));
            }

            await Task.WhenAll(tasks);
        }
    }

    private async Task<Resource> BuildResourceForProcess(
        IEndpointInfo endpointInfo,
        DiagnosticsClient client,
        OpenTelemetryOptions options,
        CancellationToken stoppingToken)
    {
        var environment = await client.GetProcessEnvironmentAsync(stoppingToken);

        var resourceAttributes = new Dictionary<string, object>();

        foreach (var resourceAttribute in options.ResourceOptions.AttributeOptions)
        {
            if (resourceAttribute.ValueOrExpression.StartsWith("$env:", StringComparison.OrdinalIgnoreCase))
            {
                var key = resourceAttribute.ValueOrExpression.Substring(5);
                if (environment.TryGetValue(key, out var value))
                {
                    resourceAttributes.Add(resourceAttribute.Key, value);
                }
                else
                {
                    _Logger.LogWarning(
                        "Resource key '{Key}' could not be resolved from environment variable '{EnvVar}' in ProcessId '{ProcessId}'.",
                        resourceAttribute.Key,
                        key,
                        endpointInfo.ProcessId);
                }
            }
            else
            {
                resourceAttributes.Add(resourceAttribute.Key, resourceAttribute.ValueOrExpression);
            }
        }

        if (!resourceAttributes.ContainsKey("service.name") && !string.IsNullOrEmpty(endpointInfo.CommandLine))
        {
            var processInfo = await ProcessInfoImpl.FromEndpointInfoAsync(endpointInfo, stoppingToken);

            resourceAttributes.Add("service.name", processInfo.ProcessName);
        }

        if (!resourceAttributes.ContainsKey("service.instance.id"))
        {
            resourceAttributes.Add("service.instance.id", endpointInfo.ProcessId.ToString());
        }

        return new Resource(resourceAttributes);
    }

    private async Task ListenToLogs(
        OpenTelemetryOptions options,
        DiagnosticsClient client,
        Resource resource,
        CancellationToken stoppingToken)
    {
        _Logger.LogTrace("Starting log collection");

        var filterSpecs = new Dictionary<string, LogLevel?>();

        foreach (var category in options.LogsOptions.CategoryOptions)
        {
            if (!Enum.TryParse(category.LogLevel, out LogLevel logLevel))
            {
                logLevel = LogLevel.Warning;
            }

            filterSpecs.Add(category.CategoryPrefix, logLevel);
        }

        if (!Enum.TryParse(options.LogsOptions.DefaultLogLevel, out LogLevel defaultLogLevel))
        {
            defaultLogLevel = LogLevel.Warning;
        }

        var settings = new EventLogsPipelineSettings
        {
            CollectScopes = options.LogsOptions.IncludeScopes,
            LogLevel = defaultLogLevel,
            UseAppFilters = true,
            FilterSpecs = filterSpecs,
            Duration = Timeout.InfiniteTimeSpan,
        };

        await using var pipeline = new EventLogsPipeline(
            client,
            settings,
            new OpenTelemetryLogRecordLogger(_LoggerFactory, resource, options));

        await pipeline.RunAsync(stoppingToken);

        _Logger.LogTrace("Stopped log collection");
    }

    private async Task ListenToMetrics(
        OpenTelemetryOptions options,
        DiagnosticsClient client,
        Version runtimeVersion,
        Resource resource,
        CancellationToken stoppingToken)
    {
        _Logger.LogTrace("Starting metrics collection");

        var counterGroups = new List<EventPipeCounterGroup>();


        foreach (var meter in options.MetricsOptions.MeterOptions)
        {
            var counterGroup = new EventPipeCounterGroup() { ProviderName = meter.MeterName, Type = CounterGroupType.Meter };

            counterGroup.CounterNames = meter.Instruments.ToArray();

            counterGroups.Add(counterGroup);
        }

        var settings = new MetricsPipelineSettings
        {
            CounterIntervalSeconds = options.MetricsOptions.PeriodicExportingOptions.ExportIntervalMilliseconds / 1000,
            CounterGroups = counterGroups.ToArray(),
            UseSharedSession = runtimeVersion?.Major >= 8,
            MaxHistograms = options.MetricsOptions.MaxHistograms,
            MaxTimeSeries = options.MetricsOptions.MaxTimeSeries,
            Duration = Timeout.InfiniteTimeSpan,
        };

        await using var pipeline = new MetricsPipeline(
            client,
            settings,
            new ICountersLogger[] { new OpenTelemetryCountersLogger(_LoggerFactory, resource, options) });

        await pipeline.RunAsync(stoppingToken);

        _Logger.LogTrace("Stopped metrics collection");
    }

    private async Task ListenToTraces(
        OpenTelemetryOptions options,
        DiagnosticsClient client,
        Version runtimeVersion,
        Resource resource,
        CancellationToken stoppingToken)
    {
        _Logger.LogTrace("Starting traces collection");

        var sources = options.TracesOptions.Sources.ToArray();

        var settings = new TracesPipelineSettings
        {
            SamplingRatio = options.TracesOptions.SamplingRatio,
            Sources = sources,
            Duration = Timeout.InfiniteTimeSpan,
        };

        await using var pipeline = new TracesPipeline(
            client,
            settings,
            new IActivityLogger[] { new OpenTelemetryActivityLogger(_LoggerFactory, resource, options) });

        await pipeline.RunAsync(stoppingToken);

        _Logger.LogTrace("Stopped traces collection");
    }
}

internal sealed class OpenTelemetryCountersLogger : MetricProducer, ICountersLogger
{
    private readonly ILoggerFactory _LoggerFactory;
    private readonly Resource _Resource;
    private readonly OpenTelemetryOptions _Options;
    private readonly MetricsStore _MetricsStore;

    private MetricReader? _MetricReader;

    public OpenTelemetryCountersLogger(
        ILoggerFactory loggerFactory,
        Resource resource,
        OpenTelemetryOptions options)
    {
        _LoggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _Resource = resource ?? throw new ArgumentNullException(nameof(resource));
        _Options = options ?? throw new ArgumentNullException(nameof(options));

        if (_Options.ExporterOptions.ExporterType != OpenTelemetryExporterType.OpenTelemetryProtocol
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
        if (_MetricReader == null)
        {
            var options = _Options.ExporterOptions.OpenTelemetryProtocolExporterOptions!;

            _MetricReader = MetricReaderFactory.CreatePeriodicExportingMetricReader(
                _LoggerFactory,
                _Resource,
                new OtlpMetricExporter(
                    _LoggerFactory.CreateLogger<OtlpMetricExporter>(),
                    _Options.ExporterOptions.OpenTelemetryProtocolExporterOptions!.ResolveOtlpExporterOptions(
                        new Uri("http://localhost:4318/v1/metrics"),
                        options.MetricsOptions)),
                new MetricProducer[] { this },
                _Options.MetricsOptions.PeriodicExportingOptions.ToOTelPeriodicExportingOptions());
        }

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

    public override bool Produce(MetricWriter writer, CancellationToken cancellationToken)
    {
        AggregationTemporality aggregationTemporality = _Options.MetricsOptions.AggregationTemporality;

        _MetricsStore.SnapshotMetrics(
            out var snapshot,
            deltaAggregation: aggregationTemporality == AggregationTemporality.Delta);

        foreach (var meter in snapshot.Meters)
        {
            writer.BeginInstrumentationScope(
                new(meter.MeterName)
                {
                    Version = meter.MeterVersion
                });

            foreach (var instrument in meter.Instruments)
            {
                Metric? otelMetric = null;

                foreach (var metricPoint in instrument.MetricPoints)
                {
                    if (otelMetric == null)
                    {
                        switch (metricPoint.EventType)
                        {
                            case EventType.Rate:
                                otelMetric = new Metric(
                                    Monitoring.OpenTelemetry.Metrics.MetricType.DoubleSum,
                                    instrument.Metadata.CounterName,
                                    aggregationTemporality)
                                {
                                    Unit = instrument.Metadata.CounterUnit,
                                    Description = instrument.Metadata.CounterDescription
                                };
                                break;
                            case EventType.Gauge:
                                otelMetric = new Metric(
                                    Monitoring.OpenTelemetry.Metrics.MetricType.DoubleGauge,
                                    instrument.Metadata.CounterName,
                                    AggregationTemporality.Cumulative)
                                {
                                    Unit = instrument.Metadata.CounterUnit,
                                    Description = instrument.Metadata.CounterDescription
                                };
                                break;
                            case EventType.UpDownCounter:
                                otelMetric = new Metric(
                                    Monitoring.OpenTelemetry.Metrics.MetricType.DoubleSumNonMonotonic,
                                    instrument.Metadata.CounterName,
                                    AggregationTemporality.Cumulative)
                                {
                                    Unit = instrument.Metadata.CounterUnit,
                                    Description = instrument.Metadata.CounterDescription
                                };
                                break;
                            case EventType.Histogram:
                                otelMetric = new Metric(
                                    Monitoring.OpenTelemetry.Metrics.MetricType.Histogram,
                                    instrument.Metadata.CounterName,
                                    aggregationTemporality)
                                {
                                    Unit = instrument.Metadata.CounterUnit,
                                    Description = instrument.Metadata.CounterDescription
                                };
                                break;
                            default:
                                return false;
                        }

                        writer.BeginMetric(otelMetric);
                    }

                    DateTime startTimeUtc = otelMetric.AggregationTemporality == AggregationTemporality.Cumulative
                        ? snapshot.ProcessStartTimeUtc
                        : snapshot.LastCollectionStartTimeUtc;
                    DateTime endTimeUtc = snapshot.LastCollectionEndTimeUtc;

                    switch (otelMetric.MetricType)
                    {
                        case Monitoring.OpenTelemetry.Metrics.MetricType.DoubleSum:
                        case Monitoring.OpenTelemetry.Metrics.MetricType.DoubleGauge:
                        case Monitoring.OpenTelemetry.Metrics.MetricType.DoubleSumNonMonotonic:
                            WriteNumberMetricPoint(writer, startTimeUtc, endTimeUtc, metricPoint);
                            break;
                        case Monitoring.OpenTelemetry.Metrics.MetricType.Histogram:
                            if (metricPoint is AggregatePercentilePayload aggregatePercentilePayload)
                            {
                                WriteHistogramMetricPoint(writer, startTimeUtc, endTimeUtc, aggregatePercentilePayload);
                            }
                            break;
                    }
                }

                if (otelMetric != null)
                {
                    writer.EndMetric();
                }
            }

            writer.EndInstrumentationScope();
        }

        return true;
    }

    private static void WriteNumberMetricPoint(
        MetricWriter writer,
        DateTime startTimeUtc,
        DateTime endTimeUtc,
        ICounterPayload payload)
    {
        double value = payload is IRatePayload ratePayload
            ? ratePayload.Rate
            : payload.Value;

        var numberMetricPoint = new NumberMetricPoint(
            startTimeUtc,
            endTimeUtc,
            value);

        writer.WriteNumberMetricPoint(
            in numberMetricPoint,
            ParseAttributes(payload));
    }

    private static void WriteHistogramMetricPoint(
        MetricWriter writer,
        DateTime startTimeUtc,
        DateTime endTimeUtc,
        AggregatePercentilePayload payload)
    {
        var histogramMetricPoint = new HistogramMetricPoint(
            startTimeUtc,
            endTimeUtc,
            features: HistogramMetricPointFeatures.None,
            min: default,
            max: default,
            payload.Sum,
            payload.Count);

        writer.WriteHistogramMetricPoint(
            in histogramMetricPoint,
            buckets: default,
            ParseAttributes(payload));
    }

    private static ReadOnlySpan<KeyValuePair<string, object?>> ParseAttributes(ICounterPayload payload)
    {
        List<KeyValuePair<string, object?>> attributes = new List<KeyValuePair<string, object?>>();

        ReadOnlySpan<char> metadata = payload.ValueTags;

        while (!metadata.IsEmpty)
        {
            int commaIndex = metadata.IndexOf(',');

            ReadOnlySpan<char> kvPair;

            if (commaIndex < 0)
            {
                kvPair = metadata;
                metadata = default;
            }
            else
            {
                kvPair = metadata[..commaIndex];
                metadata = metadata.Slice(commaIndex + 1);
            }

            int colonIndex = kvPair.IndexOf('=');
            if (colonIndex < 0)
            {
                attributes.Clear();
                break;
            }

            string metadataKey = kvPair[..colonIndex].ToString();
            string metadataValue = kvPair.Slice(colonIndex + 1).ToString();
            attributes.Add(new(metadataKey, metadataValue));
        }

        return CollectionsMarshal.AsSpan(attributes);
    }
}

internal sealed class OpenTelemetryActivityLogger : IActivityLogger
{
    private readonly ILoggerFactory _LoggerFactory;
    private readonly Resource _Resource;
    private readonly OpenTelemetryOptions _Options;

    private ISpanProcessor? _SpanProcessor;

    public OpenTelemetryActivityLogger(
        ILoggerFactory loggerFactory,
        Resource resource,
        OpenTelemetryOptions options)
    {
        _LoggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _Resource = resource ?? throw new ArgumentNullException(nameof(resource));
        _Options = options ?? throw new ArgumentNullException(nameof(options));

        if (_Options.ExporterOptions.ExporterType != OpenTelemetryExporterType.OpenTelemetryProtocol
            || _Options.ExporterOptions.OpenTelemetryProtocolExporterOptions == null)
        {
            throw new InvalidOperationException("Options were invalid.");
        }
    }

    public Task PipelineStarted(CancellationToken token)
    {
        if (_SpanProcessor == null)
        {
            var options = _Options.ExporterOptions.OpenTelemetryProtocolExporterOptions!;

            _SpanProcessor = SpanExportProcessorFactory.CreateBatchExportProcessor(
                _LoggerFactory,
                _Resource,
                new OtlpSpanExporter(
                    _LoggerFactory.CreateLogger<OtlpSpanExporter>(),
                    _Options.ExporterOptions.OpenTelemetryProtocolExporterOptions!.ResolveOtlpExporterOptions(
                        new Uri("http://localhost:4318/v1/traces"),
                        options.TracesOptions)),
                _Options.TracesOptions.BatchOptions.ToOTelBatchOptions());
        }

        return Task.CompletedTask;
    }

    public async Task PipelineStopped(CancellationToken token)
    {
        var spanProcessor = _SpanProcessor;
        if (spanProcessor != null)
        {
            await spanProcessor.ShutdownAsync(token);
            spanProcessor.Dispose();
            _SpanProcessor = null;
        }
    }

    public void Log(
        in ActivityData activity,
        ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        var spanProcessor = _SpanProcessor;
        if (spanProcessor == null)
        {
            return;
        }

        // todo: Cache InstrumentationScopes
        var scope = new InstrumentationScope(activity.Source.Name)
        {
            Version = activity.Source.Version
        };

        var spanInfo = new SpanInfo(
            scope,
            name: activity.DisplayName ?? activity.OperationName)
        {
            TraceId = activity.TraceId,
            SpanId = activity.SpanId,
            TraceFlags = activity.TraceFlags,
            TraceState = activity.TraceState,
            ParentSpanId = activity.ParentSpanId,
            Kind = activity.Kind,
            StartTimestampUtc = activity.StartTimeUtc,
            EndTimestampUtc = activity.EndTimeUtc,
            StatusCode = activity.Status,
            StatusDescription = activity.StatusDescription
        };

        var span = new Span(in spanInfo)
        {
            Attributes = tags
        };

        spanProcessor.ProcessEndedSpan(in span);
    }
}

internal sealed class OpenTelemetryLogRecordLogger : ILogRecordLogger
{
    [ThreadStatic]
    private static List<KeyValuePair<string, object?>>? s_ThreadAttributeStorage;

    private static readonly string[] LogLevels = new string[]
    {
        nameof(LogLevel.Trace),
        nameof(LogLevel.Debug),
        nameof(LogLevel.Information),
        nameof(LogLevel.Warning),
        nameof(LogLevel.Error),
        nameof(LogLevel.Critical),
        nameof(LogLevel.None),
    };

    private readonly ILoggerFactory _LoggerFactory;
    private readonly Resource _Resource;
    private readonly OpenTelemetryOptions _Options;

    private ILogRecordProcessor? _LogRecordProcessor;

    public OpenTelemetryLogRecordLogger(
        ILoggerFactory loggerFactory,
        Resource resource,
        OpenTelemetryOptions options)
    {
        _LoggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _Resource = resource ?? throw new ArgumentNullException(nameof(resource));
        _Options = options ?? throw new ArgumentNullException(nameof(options));

        if (_Options.ExporterOptions.ExporterType != OpenTelemetryExporterType.OpenTelemetryProtocol
            || _Options.ExporterOptions.OpenTelemetryProtocolExporterOptions == null)
        {
            throw new InvalidOperationException("Options were invalid.");
        }
    }

    public Task PipelineStarted(CancellationToken token)
    {
        if (_LogRecordProcessor == null)
        {
            var options = _Options.ExporterOptions.OpenTelemetryProtocolExporterOptions!;

            _LogRecordProcessor = LogRecordExportProcessorFactory.CreateBatchExportProcessor(
                _LoggerFactory,
                _Resource,
                new OtlpLogRecordExporter(
                    _LoggerFactory.CreateLogger<OtlpLogRecordExporter>(),
                    _Options.ExporterOptions.OpenTelemetryProtocolExporterOptions!.ResolveOtlpExporterOptions(
                        new Uri("http://localhost:4318/v1/logs"),
                        options.LogsOptions)),
                _Options.LogsOptions.BatchOptions.ToOTelBatchOptions());
        }

        return Task.CompletedTask;
    }

    public async Task PipelineStopped(CancellationToken token)
    {
        var logRecordProcessor = _LogRecordProcessor;
        if (logRecordProcessor != null)
        {
            await logRecordProcessor.ShutdownAsync(token);
            logRecordProcessor.Dispose();
            _LogRecordProcessor = null;
        }
    }

    public void Log(
        in Monitoring.EventPipe.LogRecord log,
        ReadOnlySpan<KeyValuePair<string, object?>> attributes,
        in LogRecordScopeContainer scopes)
    {
        var logRecordProcessor = _LogRecordProcessor;
        if (logRecordProcessor == null)
        {
            return;
        }

        // todo: Cache InstrumentationScopes
        var scope = new InstrumentationScope(log.CategoryName);

        var attributeStorage = s_ThreadAttributeStorage;
        if (attributeStorage == null)
        {
            attributeStorage = s_ThreadAttributeStorage = new(attributes.Length);
        }
        else
        {
            attributeStorage.EnsureCapacity(attributes.Length);
        }

        attributeStorage.AddRange(attributes);

        scopes.ForEachScope(ScopeCallback, ref attributeStorage);

        /* Begin: Experimental attributes not part of the OTel semantic conventions */
        if (log.EventId.Id != default)
        {
            attributeStorage.Add(new("log.record.id", log.EventId.Id));
        }

        if (!string.IsNullOrEmpty(log.EventId.Name))
        {
            attributeStorage.Add(new("log.record.name", log.EventId.Name));
        }

        if (log.MessageTemplate != null
            && log.FormattedMessage != null)
        {
            attributeStorage.Add(new("log.record.original", log.FormattedMessage));
        }
        /* End: Experimental attributes not part of the OTel semantic conventions */

        if (log.Exception != default)
        {
            if (!string.IsNullOrEmpty(log.Exception.ExceptionType))
            {
                attributeStorage.Add(new("exception.type", log.Exception.ExceptionType));
            }

            if (!string.IsNullOrEmpty(log.Exception.Message))
            {
                attributeStorage.Add(new("exception.message", log.Exception.Message));
            }

            if (!string.IsNullOrEmpty(log.Exception.StackTrace))
            {
                attributeStorage.Add(new("exception.stacktrace", log.Exception.StackTrace));
            }
        }

        LogRecordSeverity severity;
        string? severityText;
        uint intLogLevel = (uint)log.LogLevel;
        if (intLogLevel < 6)
        {
            severity = (LogRecordSeverity)((intLogLevel * 4) + 1);
            severityText = LogLevels[intLogLevel];
        }
        else
        {
            severity = LogRecordSeverity.Unspecified;
            severityText = null;
        }

        var logRecordInfo = new LogRecordInfo(scope)
        {
            TimestampUtc = log.Timestamp,
            TraceId = log.TraceId,
            SpanId = log.SpanId,
            TraceFlags = log.TraceFlags,
            Body = log.MessageTemplate ?? log.FormattedMessage,
            Severity = severity,
            SeverityText = severityText,
        };

        var logRecord = new Monitoring.OpenTelemetry.Logging.LogRecord(in logRecordInfo)
        {
            Attributes = CollectionsMarshal.AsSpan(attributeStorage)
        };

        logRecordProcessor.ProcessEmittedLogRecord(in logRecord);

        attributeStorage.Clear();

        static void ScopeCallback(
            ReadOnlySpan<KeyValuePair<string, object?>> attributes,
            ref List<KeyValuePair<string, object?>> state)
        {
            foreach (var attribute in attributes)
            {
                if (attribute.Key == "{OriginalFormat}" || string.IsNullOrEmpty(attribute.Key))
                {
                    continue;
                }

                state.Add(attribute);
            }
        }
    }
}

internal sealed class OpenTelemetryEndpointManager
{
    private readonly ILoggerFactory _LoggerFactory;
    private readonly ILogger<OpenTelemetryEndpointManager> _Logger;
    private readonly IOptionsMonitor<OpenTelemetryOptions> _OpenTelemetryOptions;
    private readonly Dictionary<int, OpenTelemetryEndpointListener> _Endpoints = new();

    public OpenTelemetryEndpointManager(
        ILoggerFactory loggerFactory,
        IOptionsMonitor<OpenTelemetryOptions> openTelemetryOptions)
    {
        _LoggerFactory = loggerFactory;
        _OpenTelemetryOptions = openTelemetryOptions;

        _Logger = loggerFactory.CreateLogger<OpenTelemetryEndpointManager>();
    }

    public void StartListeningToEndpoint(IEndpointInfo endpointInfo)
    {
        OpenTelemetryEndpointListener endpointListener;

        lock (_Endpoints)
        {
            if (_Endpoints.ContainsKey(endpointInfo.ProcessId))
            {
                _Logger.LogWarning("Process {ProcessId} connected but is already subscribed", endpointInfo.ProcessId);
                return;
            }

            _Logger.LogInformation("Process {ProcessId} connected", endpointInfo.ProcessId);

            endpointListener = new OpenTelemetryEndpointListener(
                _LoggerFactory,
                _OpenTelemetryOptions,
                endpointInfo);

            _Endpoints[endpointInfo.ProcessId] = endpointListener;
        }

        endpointListener.StartListening();
    }

    public void StopListeningToEndpoint(IEndpointInfo endpointInfo)
    {
        OpenTelemetryEndpointListener? endpointListener;

        lock (_Endpoints)
        {
            if (!_Endpoints.TryGetValue(endpointInfo.ProcessId, out endpointListener))
            {
                _Logger.LogWarning("Process {ProcessId} disconnected but a subscription could not be found", endpointInfo.ProcessId);
                return;
            }

            _Logger.LogInformation("Process {ProcessId} disconnected", endpointInfo.ProcessId);

            _Endpoints.Remove(endpointInfo.ProcessId);
        }

        endpointListener.StopListening();
    }
}

internal sealed class OpenTelemetryService : BackgroundService
{
    private readonly ILogger<OpenTelemetryService> _Logger;
    private readonly IOptionsMonitor<OpenTelemetryOptions> _OpenTelemetryOptions;
    private readonly IOptionsMonitor<ProcessFilterOptions> _ProcessOptions;
    private readonly OpenTelemetryEndpointManager _OpenTelemetryEndpointManager;
    private readonly IDiagnosticServices _DiagnosticServices;

    public OpenTelemetryService(
        ILogger<OpenTelemetryService> logger,
        IOptionsMonitor<OpenTelemetryOptions> openTelemetryOptions,
        IOptionsMonitor<ProcessFilterOptions> processOptions,
        OpenTelemetryEndpointManager openTelemetryEndpointManager,
        IDiagnosticServices diagnosticServices)
    {
        Debugger.Launch();

        _Logger = logger;
        _OpenTelemetryOptions = openTelemetryOptions;
        _ProcessOptions = processOptions;
        _OpenTelemetryEndpointManager = openTelemetryEndpointManager;
        _DiagnosticServices = diagnosticServices;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var processOptions = _ProcessOptions.Get(name: null);

            if (processOptions.Filters.Count != 1)
            {
                _Logger.LogInformation("DefaultProcess configuration was not found or was invalid OpenTelemetry monitoring will only be enabled in passive mode");

                await WaitForOptionsReloadOrStop(_ProcessOptions, stoppingToken);

                continue;
            }

            IProcessInfo processInfo;
            try
            {
                processInfo = await _DiagnosticServices.GetProcessAsync(processKey: null, stoppingToken);
            }
            catch (Exception e) when (e is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
            {
                // Note: Most likely we failed to resolve the pid. Attempt to do this again.
                await Task.Delay(5000, stoppingToken);
                continue;
            }

            stoppingToken.ThrowIfCancellationRequested();

            _OpenTelemetryEndpointManager.StartListeningToEndpoint(processInfo.EndpointInfo);

            await WaitForOptionsReloadOrStop(_ProcessOptions, stoppingToken);

            _OpenTelemetryEndpointManager.StopListeningToEndpoint(processInfo.EndpointInfo);
        }
    }

    internal static async Task WaitForOptionsReloadOrStop<T>(IOptionsMonitor<T> options, CancellationToken stoppingToken)
    {
        var cts = new TaskCompletionSource();

        using var token = options.OnChange(o => cts.TrySetResult());

        await cts.WithCancellation(stoppingToken);
    }
}
#endif
