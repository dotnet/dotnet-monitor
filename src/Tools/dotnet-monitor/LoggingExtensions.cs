// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal static class LoggingExtensions
    {
        // 1:EgressProviderAdded

        private static readonly Action<ILogger, string, Exception> _egressProviderInvalidOptions =
            LoggerMessage.Define<string>(
                eventId: new EventId(2, "EgressProviderInvalidOptions"),
                logLevel: LogLevel.Error,
                formatString: Strings.LogFormatString_EgressProviderInvalidOptions);

        // 3:EgressProviderInvalidType

        // 4:EgressProviderValidatingOptions

        private static readonly Action<ILogger, int, Exception> _egressCopyActionStreamToEgressStream =
            LoggerMessage.Define<int>(
                eventId: new EventId(5, "EgressCopyActionStreamToEgressStream"),
                logLevel: LogLevel.Debug,
                formatString: Strings.LogFormatString_EgressCopyActionStreamToEgressStream);

        private static readonly Action<ILogger, string, string, Exception> _egressProviderOptionsValidationFailure =
            LoggerMessage.Define<string, string>(
                eventId: new EventId(6, "EgressProviderOptionsValidationFailure"),
                logLevel: LogLevel.Error,
                formatString: Strings.LogFormatString_EgressProviderOptionsValidationError);

        // 7:EgressProviderOptionValue

        // 8:EgressStreamOptionValue

        // 9:EgressProviderFileName

        private static readonly Action<ILogger, string, string, Exception> _egressProviderUnableToFindPropertyKey =
            LoggerMessage.Define<string, string>(
                eventId: new EventId(10, "EgressProvideUnableToFindPropertyKey"),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_EgressProvideUnableToFindPropertyKey);

        private static readonly Action<ILogger, string, Exception> _egressProviderInvokeStreamAction =
            LoggerMessage.Define<string>(
                eventId: new EventId(11, "EgressProviderInvokeStreamAction"),
                logLevel: LogLevel.Debug,
                formatString: Strings.LogFormatString_EgressProviderInvokeStreamAction);

        private static readonly Action<ILogger, string, string, Exception> _egressProviderSavedStream =
            LoggerMessage.Define<string, string>(
                eventId: new EventId(12, "EgressProviderSavedStream"),
                logLevel: LogLevel.Debug,
                formatString: Strings.LogFormatString_EgressProviderSavedStream);

        private static readonly Action<ILogger, Exception> _noAuthentication =
            LoggerMessage.Define(
                eventId: new EventId(13, "NoAuthentication"),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_NoAuthentication);

        private static readonly Action<ILogger, Exception> _insecureAuthenticationConfiguration =
            LoggerMessage.Define(
                eventId: new EventId(14, "InsecureAutheticationConfiguration"),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_InsecureAutheticationConfiguration);

        private static readonly Action<ILogger, string, Exception> _unableToListenToAddress =
            LoggerMessage.Define<string>(
                eventId: new EventId(15, "UnableToListenToAddress"),
                logLevel: LogLevel.Error,
                formatString: Strings.LogFormatString_UnableToListenToAddress);

        private static readonly Action<ILogger, string, Exception> _boundDefaultAddress =
            LoggerMessage.Define<string>(
                eventId: new EventId(16, "BoundDefaultAddress"),
                logLevel: LogLevel.Debug,
                formatString: Strings.LogFormatString_BoundDefaultAddress);

        private static readonly Action<ILogger, string, Exception> _boundMetricsAddress =
            LoggerMessage.Define<string>(
                eventId: new EventId(17, "BoundMetricsAddress"),
                logLevel: LogLevel.Debug,
                formatString: Strings.LogFormatString_BoundMetricsAddress);

        private static readonly Action<ILogger, string, Exception> _optionsValidationFalure =
            LoggerMessage.Define<string>(
                eventId: new EventId(18, "OptionsValidationFailure"),
                logLevel: LogLevel.Critical,
                formatString: Strings.LogFormatString_OptionsValidationFailure);

        private static readonly Action<ILogger, Exception> _runningElevated =
            LoggerMessage.Define(
                eventId: new EventId(19, "RunningElevated"),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_RunningElevated);

        private static readonly Action<ILogger, Exception> _disabledNegotiateWhileElevated =
            LoggerMessage.Define(
                eventId: new EventId(20, "DisabledNegotiateWhileElevated"),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_DisabledNegotiateWhileElevated);

        private static readonly Action<ILogger, string, string, Exception> _apiKeyValidationFailure =
            LoggerMessage.Define<string, string>(
                eventId: new EventId(21, "ApiKeyValidationFailure"),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_ApiKeyValidationFailure);

        private static readonly Action<ILogger, string, Exception> _apiKeyAuthenticationOptionsChanged =
            LoggerMessage.Define<string>(
                eventId: new EventId(22, "ApiKeyAuthenticationOptionsChanged"),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_ApiKeyAuthenticationOptionsChanged);

        private static readonly Action<ILogger, string, string, string, string, Exception> _logTempKey =
            LoggerMessage.Define<string, string, string, string>(
                eventId: new EventId(23, "LogTempApiKey"),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_LogTempApiKey);

        private static readonly Action<ILogger, string, string, string, Exception> _duplicateEgressProviderIgnored =
            LoggerMessage.Define<string, string, string>(
                eventId: new EventId(24, "DuplicateEgressProviderIgnored"),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_DuplicateEgressProviderIgnored);

        private static readonly Action<ILogger, string, Exception> _duplicateCollectionRuleActionIgnored =
            LoggerMessage.Define<string>(
                eventId: new EventId(25, "DuplicateCollectionRuleActionIgnored"),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_DuplicateCollectionRuleActionIgnored);

        private static readonly Action<ILogger, string, Exception> _duplicateCollectionRuleTriggerIgnored =
            LoggerMessage.Define<string>(
                eventId: new EventId(26, "DuplicateCollectionRuleTriggerIgnored"),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_DuplicateCollectionRuleTriggerIgnored);

        public static void EgressProviderInvalidOptions(this ILogger logger, string providerName)
        {
            _egressProviderInvalidOptions(logger, providerName, null);
        }
        public static void EgressCopyActionStreamToEgressStream(this ILogger logger, int bufferSize)
        {
            _egressCopyActionStreamToEgressStream(logger, bufferSize, null);
        }

        public static void EgressProviderOptionsValidationFailure(this ILogger logger, string providerName, string failureMessage)
        {
            _egressProviderOptionsValidationFailure(logger, providerName, failureMessage, null);
        }

        public static void EgressProviderUnableToFindPropertyKey(this ILogger logger, string providerName, string keyName)
        {
            _egressProviderUnableToFindPropertyKey(logger, providerName, keyName, null);
        }

        public static void EgressProviderInvokeStreamAction(this ILogger logger, string providerName)
        {
            _egressProviderInvokeStreamAction(logger, providerName, null);
        }

        public static void EgressProviderSavedStream(this ILogger logger, string providerName, string path)
        {
            _egressProviderSavedStream(logger, providerName, path, null);
        }

        public static void NoAuthentication(this ILogger logger)
        {
            _noAuthentication(logger, null);
        }

        public static void InsecureAuthenticationConfiguration(this ILogger logger)
        {
            _insecureAuthenticationConfiguration(logger, null);
        }

        public static void UnableToListenToAddress(this ILogger logger, string address, Exception ex)
        {
            _unableToListenToAddress(logger, address, ex);
        }

        public static void BoundDefaultAddress(this ILogger logger, string address)
        {
            _boundDefaultAddress(logger, address, null);
        }

        public static void BoundMetricsAddress(this ILogger logger, string address)
        {
            _boundMetricsAddress(logger, address, null);
        }

        public static void OptionsValidationFailure(this ILogger logger, OptionsValidationException exception)
        {
            foreach (string failure in exception.Failures)
                _optionsValidationFalure(logger, failure, null);
        }

        public static void RunningElevated(this ILogger logger)
        {
            _runningElevated(logger, null);
        }

        public static void DisabledNegotiateWhileElevated(this ILogger logger)
        {
            _disabledNegotiateWhileElevated(logger, null);
        }

        public static void ApiKeyValidationFailures(this ILogger logger, IEnumerable<ValidationResult> errors)
        {
            foreach (ValidationResult error in errors)
            {
                _apiKeyValidationFailure(logger, nameof(ConfigurationKeys.ApiAuthentication), error.ErrorMessage, null);
            }
        }

        public static void ApiKeyAuthenticationOptionsChanged(this ILogger logger)
        {
            _apiKeyAuthenticationOptionsChanged(logger, nameof(ConfigurationKeys.ApiAuthentication), null);
        }

        public static void LogTempKey(this ILogger logger, string monitorApiKey)
        {
            _logTempKey(logger, Environment.NewLine, HeaderNames.Authorization, Monitoring.WebApi.AuthConstants.ApiKeySchema, monitorApiKey, null);
        }

        public static void DuplicateEgressProviderIgnored(this ILogger logger, string providerName, string providerType, string existingProviderType)
        {
            _duplicateEgressProviderIgnored(logger, providerName, providerType, existingProviderType, null);
        }

        public static void DuplicateCollectionRuleActionIgnored(this ILogger logger, string actionType)
        {
            _duplicateCollectionRuleActionIgnored(logger, actionType, null);
        }

        public static void DuplicateCollectionRuleTriggerIgnored(this ILogger logger, string triggerType)
        {
            _duplicateCollectionRuleTriggerIgnored(logger, triggerType, null);
        }
    }
}
