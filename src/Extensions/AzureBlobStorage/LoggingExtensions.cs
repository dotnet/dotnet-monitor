// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.Diagnostics.Monitoring.AzureBlobStorage
{
    public static class LoggingExtensions
    {
        private static readonly Action<ILogger, int, Exception> _egressCopyActionStreamToEgressStream =
            LoggerMessage.Define<int>(
                eventId: LoggingEventIds.EgressCopyActionStreamToEgressStream.EventId(),
                logLevel: LogLevel.Debug,
                formatString: Strings.LogFormatString_EgressCopyActionStreamToEgressStream);

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

        private static readonly Action<ILogger, string, string, string, Exception> _queueDoesNotExist =
            LoggerMessage.Define<string, string, string>(
                eventId: LoggingEventIds.QueueDoesNotExist.EventId(),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_QueueDoesNotExist);

        private static readonly Action<ILogger, string, Exception> _writingMessageToQueueFailed =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.WritingMessageToQueueFailed.EventId(),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_WritingMessageToQueueFailed);

        private static readonly Action<ILogger, string, string, Exception> _queueOptionsPartiallySet =
            LoggerMessage.Define<string, string>(
                eventId: LoggingEventIds.QueueOptionsPartiallySet.EventId(),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_QueueOptionsPartiallySet);

        private static readonly Action<ILogger, Exception> _invalidMetadata =
            LoggerMessage.Define(
                eventId: LoggingEventIds.InvalidMetadata.EventId(),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_InvalidMetadata);

        private static readonly Action<ILogger, string, Exception> _duplicateKeyInMetadata =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.DuplicateKeyInMetadata.EventId(),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_DuplicateKeyInMetadata);

        private static readonly Action<ILogger, string, Exception> _environmentVariableNotFound =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.EnvironmentVariableNotFound.EventId(),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_EnvironmentVariableNotFound);

        private static readonly Action<ILogger, Exception> _environmentBlockNotSupported =
            LoggerMessage.Define(
                eventId: LoggingEventIds.EnvironmentBlockNotSupported.EventId(),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_EnvironmentBlockNotSupported);

        public static void EgressCopyActionStreamToEgressStream(this ILogger logger, int bufferSize)
        {
            _egressCopyActionStreamToEgressStream(logger, bufferSize, null);
        }

        public static void EgressProviderInvokeStreamAction(this ILogger logger, string providerName)
        {
            _egressProviderInvokeStreamAction(logger, providerName, null);
        }

        public static void EgressProviderSavedStream(this ILogger logger, string providerName, string path)
        {
            _egressProviderSavedStream(logger, providerName, path, null);
        }

        public static void QueueDoesNotExist(this ILogger logger, string queueName)
        {
            _queueDoesNotExist(logger, queueName, nameof(AzureBlobEgressProviderOptions.QueueName), nameof(AzureBlobEgressProviderOptions.QueueAccountUri), null);
        }

        public static void WritingMessageToQueueFailed(this ILogger logger, string queueName, Exception ex)
        {
            _writingMessageToQueueFailed(logger, queueName, ex);
        }

        public static void QueueOptionsPartiallySet(this ILogger logger)
        {
            _queueOptionsPartiallySet(logger, nameof(AzureBlobEgressProviderOptions.QueueName), nameof(AzureBlobEgressProviderOptions.QueueAccountUri), null);
        }

        public static void InvalidMetadata(this ILogger logger, Exception ex)
        {
            _invalidMetadata(logger, ex);
        }

        public static void DuplicateKeyInMetadata(this ILogger logger, string duplicateKey)
        {
            _duplicateKeyInMetadata(logger, duplicateKey, null);
        }

        public static void EnvironmentVariableNotFound(this ILogger logger, string environmentVariable)
        {
            _environmentVariableNotFound(logger, environmentVariable, null);
        }

        public static void EnvironmentBlockNotSupported(this ILogger logger)
        {
            _environmentBlockNotSupported(logger, null);
        }

        public static void EgressProviderUnableToFindPropertyKey(this ILogger logger, string providerName, string keyName)
        {
            _egressProviderUnableToFindPropertyKey(logger, providerName, keyName, null);
        }
    }
}
