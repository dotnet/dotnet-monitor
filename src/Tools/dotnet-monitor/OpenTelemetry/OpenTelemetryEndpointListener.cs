// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if BUILDING_OTEL
#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tools.Monitor.OpenTelemetry.Logging;
using Microsoft.Diagnostics.Tools.Monitor.OpenTelemetry.Metrics;
using Microsoft.Diagnostics.Tools.Monitor.OpenTelemetry.Tracing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Configuration;
using OpenTelemetry.Resources;

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

            var logsEnabled = options.LoggingOptions.DefaultLogLevel != null
                || options.LoggingOptions.CategoryOptions.Any();
            var metricsEnabled = options.MetricsOptions.MeterOptions.Any();
            var tracingEnabled = options.TracingOptions.Sources.Any();

            if (!logsEnabled && !metricsEnabled && !tracingEnabled)
            {
                _Logger.LogInformation("No OpenTelemetry configuration found process will not be monitored");
                await OpenTelemetryService.WaitForOptionsReloadOrStop(_OpenTelemetryOptions, stoppingToken);
                continue;
            }

            if (options.ExporterOptions.ExporterType != "OpenTelemetryProtocol")
            {
                _Logger.LogInformation("OpenTelemetry configuration ExporterType '{ExporterType}' is not supported", options.ExporterOptions.ExporterType);
                await OpenTelemetryService.WaitForOptionsReloadOrStop(_OpenTelemetryOptions, stoppingToken);
                continue;
            }

            var client = new DiagnosticsClient(_EndpointInfo.Endpoint);

            var resource = await BuildResourceForProcess(_EndpointInfo, client, options, stoppingToken);

            _Logger.LogInformation("Resource created for process with {NumberOfKeys} keys.", resource.Attributes.Length);

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
                    ListenToTraces(options, client, resource, linkedTokenSource.Token));
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

        var processInfo = await ProcessInfoImpl.FromEndpointInfoAsync(endpointInfo, stoppingToken);

        var resource = OpenTelemetryFactory.CreateResource(
            options.ResourceOptions,
            out var unresolvedAttributes,
            environment,
            processInfo.ProcessName,
            endpointInfo.ProcessId.ToString());

        foreach (var attribute in unresolvedAttributes)
        {
            _Logger.LogWarning(
                "Resource key '{Key}' could not be resolved from value/expression '{ValueOrExpression}' in ProcessId '{ProcessId}'.",
                attribute.Key,
                attribute.ValueOrExpression,
                endpointInfo.ProcessId);
        }

        return resource;
    }

    private async Task ListenToLogs(
        OpenTelemetryOptions options,
        DiagnosticsClient client,
        Resource resource,
        CancellationToken stoppingToken)
    {
        _Logger.LogTrace("Starting log collection");

        var filterSpecs = new Dictionary<string, LogLevel?>();

        foreach (var category in options.LoggingOptions.CategoryOptions)
        {
            if (!Enum.TryParse(category.LogLevel, out LogLevel logLevel))
            {
                logLevel = LogLevel.Warning;
            }

            filterSpecs.Add(category.CategoryPrefix, logLevel);
        }

        if (!Enum.TryParse(options.LoggingOptions.DefaultLogLevel, out LogLevel defaultLogLevel))
        {
            defaultLogLevel = LogLevel.Warning;
        }

        var settings = new EventLogsPipelineSettings
        {
            CollectScopes = options.LoggingOptions.IncludeScopes,
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
        Version? runtimeVersion,
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
        Resource resource,
        CancellationToken stoppingToken)
    {
        _Logger.LogTrace("Starting traces collection");

        var sources = options.TracingOptions.Sources.ToArray();

        double samplingRatio = options.TracingOptions.SamplerOptions.SamplerType == "ParentBased"
            && options.TracingOptions.SamplerOptions.ParentBasedOptions?.RootSamplerOptions?.SamplerType == "TraceIdRatio"
            ? options.TracingOptions.SamplerOptions.ParentBasedOptions.RootSamplerOptions.TraceIdRatioBasedOptions?.SamplingRatio ?? 1.0D
            : 1.0D;

        var settings = new DistributedTracesPipelineSettings
        {
            SamplingRatio = samplingRatio,
            Sources = sources,
            Duration = Timeout.InfiniteTimeSpan,
        };

        await using var pipeline = new DistributedTracesPipeline(
            client,
            settings,
            new IActivityLogger[] { new OpenTelemetryActivityLogger(_LoggerFactory, resource, options) });

        await pipeline.RunAsync(stoppingToken);

        _Logger.LogTrace("Stopped traces collection");
    }
}
#endif
