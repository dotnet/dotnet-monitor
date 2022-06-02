﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.Monitor.Egress.AzureBlob;

namespace Microsoft.Diagnostics.Tools.Monitor.Egress
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
    }
}
