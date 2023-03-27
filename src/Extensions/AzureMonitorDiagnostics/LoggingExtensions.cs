// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.Diagnostics.Monitoring.AzureMonitorDiagnostics;

internal static class LoggingExtensions
{
    private static readonly Action<ILogger, string, Exception?> _egressProviderInvokeStreamAction =
        LoggerMessage.Define<string>(
            eventId: LoggingEventIds.EgressProviderInvokeStreamAction.EventId(),
            logLevel: LogLevel.Debug,
            formatString: Strings.LogFormatString_EgressProviderInvokeStreamAction);

    private static readonly Action<ILogger, Exception?> _invalidMetadata =
        LoggerMessage.Define(
            eventId: LoggingEventIds.InvalidMetadata.EventId(),
            logLevel: LogLevel.Warning,
            formatString: Strings.LogFormatString_InvalidMetadata);

    private static readonly Action<ILogger, string, Exception?> _duplicateKeyInMetadata =
        LoggerMessage.Define<string>(
            eventId: LoggingEventIds.DuplicateKeyInMetadata.EventId(),
            logLevel: LogLevel.Warning,
            formatString: Strings.LogFormatString_DuplicateKeyInMetadata);

    private static readonly Action<ILogger, string, string, Exception?> _egressProviderSavedStream =
        LoggerMessage.Define<string, string>(
            eventId: LoggingEventIds.EgressProviderSavedStream.EventId(),
            logLevel: LogLevel.Debug,
            formatString: Strings.LogFormatString_EgressProviderSavedStream);

    public static void EgressProviderInvokeStreamAction(this ILogger logger, string providerName)
        => _egressProviderInvokeStreamAction(logger, providerName, null);

    public static void EgressProviderSavedStream(this ILogger logger, string providerName, string path)
        => _egressProviderSavedStream(logger, providerName, path, null);

    public static void InvalidMetadata(this ILogger logger, Exception ex)
        => _invalidMetadata(logger, ex);

    public static void DuplicateKeyInMetadata(this ILogger logger, string duplicateKey)
        => _duplicateKeyInMetadata(logger, duplicateKey, null);
}
