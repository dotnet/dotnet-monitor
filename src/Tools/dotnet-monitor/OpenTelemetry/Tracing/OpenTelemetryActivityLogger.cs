// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if BUILDING_OTEL
#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Extensions.Logging;

using OpenTelemetry;
using OpenTelemetry.Configuration;
using OpenTelemetry.Resources;
using OpenTelemetry.Tracing;

namespace Microsoft.Diagnostics.Tools.Monitor.OpenTelemetry.Tracing;

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

        if (_Options.ExporterOptions.ExporterType != "OpenTelemetryProtocol"
            || _Options.ExporterOptions.OpenTelemetryProtocolExporterOptions == null)
        {
            throw new InvalidOperationException("Options were invalid.");
        }
    }

    public Task PipelineStarted(CancellationToken token)
    {
        try
        {
            _SpanProcessor ??= OpenTelemetryFactory.CreateSpanBatchExportProcessorAsync(
                _LoggerFactory,
                _Resource,
                _Options.ExporterOptions,
                _Options.TracingOptions.BatchOptions);
        }
        catch (Exception ex)
        {
            _LoggerFactory.CreateLogger<OpenTelemetryActivityLogger>()
                .LogError(ex, "OpenTelemetryActivityLogger failed to initialize OpenTelemetry SDK");
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
#endif
