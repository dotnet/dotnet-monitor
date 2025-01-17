// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Tracing;
using Microsoft.Extensions.Logging;
using System;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal static class LoggingExtensions
    {
        private static readonly Action<ILogger, Exception> _requestFailed =
            LoggerMessage.Define(
                eventId: new EventId(1, "RequestFailed"),
                logLevel: LogLevel.Error,
                formatString: Strings.LogFormatString_RequestFailed);

        private static readonly Action<ILogger, Exception?> _requestCanceled =
            LoggerMessage.Define(
                eventId: new EventId(2, "RequestCanceled"),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_RequestCanceled);

        private static readonly Action<ILogger, Exception?> _resolvedTargetProcess =
            LoggerMessage.Define(
                eventId: new EventId(3, "ResolvedTargetProcess"),
                logLevel: LogLevel.Debug,
                formatString: Strings.LogFormatString_ResolvedTargetProcess);

        private static readonly Action<ILogger, string, Exception?> _egressedArtifact =
            LoggerMessage.Define<string>(
                eventId: new EventId(4, "EgressedArtifact"),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_EgressedArtifact);

        private static readonly Action<ILogger, Exception?> _writtenToHttpStream =
            LoggerMessage.Define(
                eventId: new EventId(5, "WrittenToHttpStream"),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_WrittenToHttpStream);

        private static readonly Action<ILogger, int, int, Exception?> _throttledEndpoint =
            LoggerMessage.Define<int, int>(
                eventId: new EventId(6, "ThrottledEndpoint"),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_ThrottledEndpoint);

        private static readonly Action<ILogger, Exception> _defaultProcessUnexpectedFailure =
            LoggerMessage.Define(
                eventId: new EventId(7, "DefaultProcessUnexpectedFailure"),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_DefaultProcessUnexpectedFailure);

        private static readonly Action<ILogger, string, string, Exception?> _stoppingTraceEventHit =
            LoggerMessage.Define<string, string>(
                eventId: new EventId(8, "StoppingTraceEventHit"),
                logLevel: LogLevel.Debug,
                formatString: Strings.LogFormatString_StoppingTraceEventHit);

        private static readonly Action<ILogger, string, string, string, Exception?> _stoppingTraceEventPayloadFilterMismatch =
            LoggerMessage.Define<string, string, string>(
                eventId: new EventId(9, "StoppingTraceEventPayloadFilterMismatch"),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_StoppingTraceEventPayloadFilterMismatch);

        private static readonly Action<ILogger, int, Exception> _diagnosticRequestFailed =
            LoggerMessage.Define<int>(
                eventId: new EventId(10, "DiagnosticRequestFailed"),
                logLevel: LogLevel.Debug,
                formatString: Strings.LogFormatString_DiagnosticRequestFailed);

        private static readonly Action<ILogger, Guid, Exception> _stopOperationFailed =
            LoggerMessage.Define<Guid>(
                eventId: new EventId(11, "StopOperationFailed"),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_StopOperationFailed);

        private static readonly Action<ILogger, long, Exception?> _metricsDropped =
            LoggerMessage.Define<long>(
                eventId: new EventId(12, "MetricsDropped"),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_MetricsDropped);

        private static readonly Action<ILogger, Exception> _metricsWriteFailed =
            LoggerMessage.Define(
                eventId: new EventId(13, "MetricsWriteFailed"),
                logLevel: LogLevel.Error,
                formatString: Strings.LogFormatString_MetricsWriteFailed);

        private static readonly Action<ILogger, Exception?> _metricsAbandonCompletion =
            LoggerMessage.Define(
                eventId: new EventId(14, "MetricsAbandonCompletion"),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_MetricsAbandonCompletion);

        private static readonly Action<ILogger, int, Exception?> _metricsUnprocessed =
            LoggerMessage.Define<int>(
                eventId: new EventId(15, "MetricsUnprocessed"),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_MetricsUnprocessed);

        private static readonly Action<ILogger, string, Exception?> _counterEndedPayload =
            LoggerMessage.Define<string>(
                eventId: new EventId(16, "CounterEndedPayload"),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_CounterEndedPayload);

        private static readonly Action<ILogger, string, Exception?> _errorPayload =
            LoggerMessage.Define<string>(
                eventId: new EventId(17, "ErrorPayload"),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_ErrorPayload);

        private static readonly Action<ILogger, Exception?> _generatedInProcessArtifact =
            LoggerMessage.Define(
                eventId: new EventId(18, "GeneratedInProcessArtifact"),
                logLevel: LogLevel.Information,
                formatString: Strings.Message_GeneratedInProcessArtifact);

        public static void RequestFailed(this ILogger logger, Exception ex)
        {
            _requestFailed(logger, ex);
        }

        public static void RequestCanceled(this ILogger logger)
        {
            _requestCanceled(logger, null);
        }

        public static void ResolvedTargetProcess(this ILogger logger)
        {
            _resolvedTargetProcess(logger, null);
        }

        public static void EgressedArtifact(this ILogger logger, string location)
        {
            _egressedArtifact(logger, location, null);
        }

        public static void ThrottledEndpoint(this ILogger logger, int limit, int requests)
        {
            _throttledEndpoint(logger, limit, requests, null);
        }

        public static void WrittenToHttpStream(this ILogger logger)
        {
            _writtenToHttpStream(logger, null);
        }

        public static void DefaultProcessUnexpectedFailure(this ILogger logger, Exception ex)
        {
            _defaultProcessUnexpectedFailure(logger, ex);
        }

        public static void StoppingTraceEventHit(this ILogger logger, TraceEvent traceEvent)
        {
            _stoppingTraceEventHit(logger, traceEvent.ProviderName, traceEvent.EventName, null);
        }

        public static void StoppingTraceEventPayloadFilterMismatch(this ILogger logger, TraceEvent traceEvent)
        {
            _stoppingTraceEventPayloadFilterMismatch(logger, traceEvent.ProviderName, traceEvent.EventName, string.Join(' ', traceEvent.PayloadNames), null);
        }

        public static void DiagnosticRequestFailed(this ILogger logger, int processId, Exception ex)
        {
            _diagnosticRequestFailed(logger, processId, ex);
        }

        public static void StopOperationFailed(this ILogger logger, Guid operationId, Exception ex)
        {
            _stopOperationFailed(logger, operationId, ex);
        }

        public static void MetricsDropped(this ILogger logger, long count)
        {
            _metricsDropped(logger, count, null);
        }

        public static void MetricsWriteFailed(this ILogger logger, Exception ex)
        {
            _metricsWriteFailed(logger, ex);
        }

        public static void MetricsAbandonCompletion(this ILogger logger)
        {
            _metricsAbandonCompletion(logger, null);
        }

        public static void MetricsUnprocessed(this ILogger logger, int count)
        {
            _metricsUnprocessed(logger, count, null);
        }

        public static void CounterEndedPayload(this ILogger logger, string counterName)
        {
            _counterEndedPayload(logger, counterName, null);
        }

        public static void ErrorPayload(this ILogger logger, string message)
        {
            _errorPayload(logger, message, null);
        }

        public static void GeneratedInProcessArtifact(this ILogger logger)
        {
            _generatedInProcessArtifact(logger, null);
        }
    }
}
