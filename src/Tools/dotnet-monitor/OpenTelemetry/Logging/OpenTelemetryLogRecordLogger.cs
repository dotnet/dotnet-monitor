// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if BUILDING_OTEL
#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Extensions.Logging;

using OpenTelemetry;
using OpenTelemetry.Configuration;
using OpenTelemetry.Logging;
using OpenTelemetry.Resources;

namespace Microsoft.Diagnostics.Tools.Monitor.OpenTelemetry.Logging;

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

        if (_Options.ExporterOptions.ExporterType != "OpenTelemetryProtocol"
            || _Options.ExporterOptions.OpenTelemetryProtocolExporterOptions == null)
        {
            throw new InvalidOperationException("Options were invalid.");
        }
    }

    public Task PipelineStarted(CancellationToken token)
    {
        _LogRecordProcessor ??= OpenTelemetryFactory.CreateLogRecordBatchExportProcessorAsync(
            _LoggerFactory,
            _Resource,
            _Options.ExporterOptions,
            _Options.LoggingOptions.BatchOptions);

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
            severity = (LogRecordSeverity)(intLogLevel * 4 + 1);
            severityText = LogLevels[intLogLevel];
        }
        else
        {
            severity = LogRecordSeverity.Unspecified;
            severityText = null;
        }

        var spanContext = new ActivityContext(log.TraceId, log.SpanId, log.TraceFlags);

        var logRecordInfo = new LogRecordInfo(scope)
        {
            TimestampUtc = log.Timestamp,
            Body = log.MessageTemplate ?? log.FormattedMessage,
            Severity = severity,
            SeverityText = severityText,
        };

        var logRecord = new global::OpenTelemetry.Logging.LogRecord(
            in spanContext,
            in logRecordInfo)
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
#endif
