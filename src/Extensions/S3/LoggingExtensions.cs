// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.Monitoring.S3
{
    public static class LoggingExtensions
    {
        private static readonly Action<ILogger, string, Exception> _egressProviderInvokeStreamAction =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.EgressProviderInvokeStreamAction.EventId(),
                logLevel: LogLevel.Debug,
                formatString: Strings.LogFormatString_EgressProviderInvokeStreamAction);

        private static readonly Action<ILogger, string, string, Exception> _egressProviderSavedStream =
            LoggerMessage.Define<string, string>(
                eventId: LoggingEventIds.EgressProviderSavedStream.EventId(),
                logLevel: LogLevel.Debug,
                formatString: Strings.LogFormatString_EgressProviderSavedStream);

        private static readonly Action<ILogger, string, string, Exception> _egressProviderUnableToFindPropertyKey =
            LoggerMessage.Define<string, string>(
                eventId: LoggingEventIds.EgressProvideUnableToFindPropertyKey.EventId(),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_EgressProviderUnableToFindPropertyKey);

        public static void EgressProviderInvokeStreamAction(this ILogger logger, string providerName)
        {
            _egressProviderInvokeStreamAction(logger, providerName, null);
        }

        public static void EgressProviderSavedStream(this ILogger logger, string providerName, string path)
        {
            _egressProviderSavedStream(logger, providerName, path, null);
        }

        public static void EgressProviderUnableToFindPropertyKey(this ILogger logger, string providerName, string keyName)
        {
            _egressProviderUnableToFindPropertyKey(logger, providerName, keyName, null);
        }
    }
}
