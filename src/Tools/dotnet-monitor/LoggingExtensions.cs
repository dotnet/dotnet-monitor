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
        private static readonly Action<ILogger, string, Exception> _egressProviderInvalidOptions =
            LoggerMessage.Define<string>(
                eventId: new EventId(LoggingEventIds.EgressProviderInvalidOptions, "EgressProviderInvalidOptions"),
                logLevel: LogLevel.Error,
                formatString: Strings.LogFormatString_EgressProviderInvalidOptions);

        private static readonly Action<ILogger, int, Exception> _egressCopyActionStreamToEgressStream =
            LoggerMessage.Define<int>(
                eventId: new EventId(LoggingEventIds.EgressCopyActionStreamToEgressStream, "EgressCopyActionStreamToEgressStream"),
                logLevel: LogLevel.Debug,
                formatString: Strings.LogFormatString_EgressCopyActionStreamToEgressStream);

        private static readonly Action<ILogger, string, string, Exception> _egressProviderOptionsValidationFailure =
            LoggerMessage.Define<string, string>(
                eventId: new EventId(LoggingEventIds.EgressProviderOptionsValidationFailure, "EgressProviderOptionsValidationFailure"),
                logLevel: LogLevel.Error,
                formatString: Strings.LogFormatString_EgressProviderOptionsValidationError);

        private static readonly Action<ILogger, string, string, Exception> _egressProviderUnableToFindPropertyKey =
            LoggerMessage.Define<string, string>(
                eventId: new EventId(LoggingEventIds.EgressProvideUnableToFindPropertyKey, "EgressProvideUnableToFindPropertyKey"),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_EgressProvideUnableToFindPropertyKey);

        private static readonly Action<ILogger, string, Exception> _egressProviderInvokeStreamAction =
            LoggerMessage.Define<string>(
                eventId: new EventId(LoggingEventIds.EgressProviderInvokeStreamAction, "EgressProviderInvokeStreamAction"),
                logLevel: LogLevel.Debug,
                formatString: Strings.LogFormatString_EgressProviderInvokeStreamAction);

        private static readonly Action<ILogger, string, string, Exception> _egressProviderSavedStream =
            LoggerMessage.Define<string, string>(
                eventId: new EventId(LoggingEventIds.EgressProviderSavedStream, "EgressProviderSavedStream"),
                logLevel: LogLevel.Debug,
                formatString: Strings.LogFormatString_EgressProviderSavedStream);

        private static readonly Action<ILogger, Exception> _noAuthentication =
            LoggerMessage.Define(
                eventId: new EventId(LoggingEventIds.NoAuthentication, "NoAuthentication"),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_NoAuthentication);

        private static readonly Action<ILogger, Exception> _insecureAuthenticationConfiguration =
            LoggerMessage.Define(
                eventId: new EventId(LoggingEventIds.InsecureAutheticationConfiguration, "InsecureAutheticationConfiguration"),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_InsecureAutheticationConfiguration);

        private static readonly Action<ILogger, string, Exception> _unableToListenToAddress =
            LoggerMessage.Define<string>(
                eventId: new EventId(LoggingEventIds.UnableToListenToAddress, "UnableToListenToAddress"),
                logLevel: LogLevel.Error,
                formatString: Strings.LogFormatString_UnableToListenToAddress);

        private static readonly Action<ILogger, string, Exception> _boundDefaultAddress =
            LoggerMessage.Define<string>(
                eventId: new EventId(LoggingEventIds.BoundDefaultAddress, "BoundDefaultAddress"),
                logLevel: LogLevel.Debug,
                formatString: Strings.LogFormatString_BoundDefaultAddress);

        private static readonly Action<ILogger, string, Exception> _boundMetricsAddress =
            LoggerMessage.Define<string>(
                eventId: new EventId(LoggingEventIds.BoundMetricsAddress, "BoundMetricsAddress"),
                logLevel: LogLevel.Debug,
                formatString: Strings.LogFormatString_BoundMetricsAddress);

        private static readonly Action<ILogger, string, Exception> _optionsValidationFalure =
            LoggerMessage.Define<string>(
                eventId: new EventId(LoggingEventIds.OptionsValidationFailure, "OptionsValidationFailure"),
                logLevel: LogLevel.Critical,
                formatString: Strings.LogFormatString_OptionsValidationFailure);

        private static readonly Action<ILogger, Exception> _runningElevated =
            LoggerMessage.Define(
                eventId: new EventId(LoggingEventIds.RunningElevated, "RunningElevated"),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_RunningElevated);

        private static readonly Action<ILogger, Exception> _disabledNegotiateWhileElevated =
            LoggerMessage.Define(
                eventId: new EventId(LoggingEventIds.DisabledNegotiateWhileElevated, "DisabledNegotiateWhileElevated"),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_DisabledNegotiateWhileElevated);

        private static readonly Action<ILogger, string, string, Exception> _apiKeyValidationFailure =
            LoggerMessage.Define<string, string>(
                eventId: new EventId(LoggingEventIds.ApiKeyValidationFailure, "ApiKeyValidationFailure"),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_ApiKeyValidationFailure);

        private static readonly Action<ILogger, string, string, string, string, Exception> _logTempKey =
            LoggerMessage.Define<string, string, string, string>(
                eventId: new EventId(LoggingEventIds.LogTempApiKey, "LogTempApiKey"),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_LogTempApiKey);

        private static readonly Action<ILogger, string, string, string, Exception> _duplicateEgressProviderIgnored =
            LoggerMessage.Define<string, string, string>(
                eventId: new EventId(LoggingEventIds.DuplicateEgressProviderIgnored, "DuplicateEgressProviderIgnored"),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_DuplicateEgressProviderIgnored);

        private static readonly Action<ILogger, string, Exception> _apiKeyAuthenticationOptionsValidated =
            LoggerMessage.Define<string>(
                eventId: new EventId(LoggingEventIds.ApiKeyAuthenticationOptionsValidated, "ApiKeyAuthenticationOptionsValidated"),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_ApiKeyAuthenticationOptionsValidated);

        private static readonly Action<ILogger, string, Exception> _notifyPrivateKey =
            LoggerMessage.Define<string>(
                eventId: new EventId(LoggingEventIds.NotifyPrivateKey, "NotifyPrivateKey"),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_NotifyPrivateKey);

        private static readonly Action<ILogger, string, Exception> _duplicateCollectionRuleActionIgnored =
            LoggerMessage.Define<string>(
                eventId: new EventId(LoggingEventIds.DuplicateCollectionRuleActionIgnored, "DuplicateCollectionRuleActionIgnored"),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_DuplicateCollectionRuleActionIgnored);

        private static readonly Action<ILogger, string, Exception> _duplicateCollectionRuleTriggerIgnored =
            LoggerMessage.Define<string>(
                eventId: new EventId(LoggingEventIds.DuplicateCollectionRuleTriggerIgnored, "DuplicateCollectionRuleTriggerIgnored"),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_DuplicateCollectionRuleTriggerIgnored);

        private static readonly Action<ILogger, string, Exception> _collectionRuleStarted =
            LoggerMessage.Define<string>(
                eventId: new EventId(LoggingEventIds.CollectionRuleStarted, "CollectionRuleStarted"),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_CollectionRuleStarted);

        private static readonly Action<ILogger, string, Exception> _collectionRuleFailed =
            LoggerMessage.Define<string>(
                eventId: new EventId(LoggingEventIds.CollectionRuleFailed, "CollectionRuleFailed"),
                logLevel: LogLevel.Error,
                formatString: Strings.LogFormatString_CollectionRuleFailed);

        private static readonly Action<ILogger, string, Exception> _collectionRuleCompleted =
            LoggerMessage.Define<string>(
                eventId: new EventId(LoggingEventIds.CollectionRuleCompleted, "CollectionRuleCompleted"),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_CollectionRuleCompleted);

        private static readonly Action<ILogger, Exception> _collectionRulesStarted =
            LoggerMessage.Define(
                eventId: new EventId(LoggingEventIds.CollectionRulesStarted, "CollectionRulesStarted"),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_CollectionRulesStarted);

        private static readonly Action<ILogger, string, string, Exception> _collectionRuleActionStarted =
            LoggerMessage.Define<string, string>(
                eventId: new EventId(LoggingEventIds.CollectionRuleActionStarted, "CollectionRuleActionStarted"),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_CollectionRuleActionStarted);

        private static readonly Action<ILogger, string, string, Exception> _collectionRuleActionCompleted =
            LoggerMessage.Define<string, string>(
                eventId: new EventId(LoggingEventIds.CollectionRuleActionCompleted, "CollectionRuleActionCompleted"),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_CollectionRuleActionCompleted);

        private static readonly Action<ILogger, string, string, Exception> _collectionRuleTriggerStarted =
            LoggerMessage.Define<string, string>(
                eventId: new EventId(LoggingEventIds.CollectionRuleTriggerStarted, "CollectionRuleTriggerStarted"),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_CollectionRuleTriggerStarted);

        private static readonly Action<ILogger, string, string, Exception> _collectionRuleTriggerCompleted =
            LoggerMessage.Define<string, string>(
                eventId: new EventId(LoggingEventIds.CollectionRuleTriggerCompleted, "CollectionRuleTriggerCompleted"),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_CollectionRuleTriggerCompleted);

        private static readonly Action<ILogger, string, Exception> _collectionRuleActionsThrottled =
            LoggerMessage.Define<string>(
                eventId: new EventId(LoggingEventIds.CollectionRuleActionsThrottled, "CollectionRuleActionsThrottled"),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_CollectionRuleActionsThrottled);

        private static readonly Action<ILogger, string, string, Exception> _collectionRuleActionFailed =
            LoggerMessage.Define<string, string>(
                eventId: new EventId(LoggingEventIds.CollectionRuleActionFailed, "CollectionRuleActionFailed"),
                logLevel: LogLevel.Error,
                formatString: Strings.LogFormatString_CollectionRuleActionFailed);

        private static readonly Action<ILogger, string, Exception> _collectionRuleActionsCompleted =
            LoggerMessage.Define<string>(
                eventId: new EventId(LoggingEventIds.CollectionRuleActionsCompleted, "CollectionRuleActionsCompleted"),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_CollectionRuleActionsCompleted);

        private static readonly Action<ILogger, Exception> _collectionRulesStarting =
            LoggerMessage.Define(
                eventId: new EventId(LoggingEventIds.CollectionRulesStarting, "CollectionRulesStarting"),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_CollectionRulesStarting);

        private static readonly Action<ILogger, int, Exception> _diagnosticRequestCancelled =
            LoggerMessage.Define<int>(
                eventId: new EventId(LoggingEventIds.DiagnosticRequestCancelled, "DiagnosticRequestCancelled"),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_DiagnosticRequestCancelled);

        private static readonly Action<ILogger, string, Exception> _collectionRuleUnmatchedFilters =
            LoggerMessage.Define<string>(
                eventId: new EventId(LoggingEventIds.CollectionRuleUnmatchedFilters, "CollectionRuleUnmatchedFilters"),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_CollectionRuleUnmatchedFilters);

        private static readonly Action<ILogger, Exception> _collectionRuleConfigurationChanged =
            LoggerMessage.Define(
                eventId: new EventId(LoggingEventIds.CollectionRuleConfigurationChanged, "CollectionRuleConfigurationChanged"),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_CollectionRuleConfigurationChanged);

        private static readonly Action<ILogger, Exception> _collectionRulesStopping =
            LoggerMessage.Define(
                eventId: new EventId(LoggingEventIds.CollectionRulesStopping, "CollectionRulesStopping"),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_CollectionRulesStopping);

        private static readonly Action<ILogger, Exception> _collectionRulesStopped =
            LoggerMessage.Define(
                eventId: new EventId(LoggingEventIds.CollectionRulesStopped, "CollectionRulesStopped"),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_CollectionRulesStopped);

        private static readonly Action<ILogger, string, Exception> _collectionRuleCancelled =
            LoggerMessage.Define<string>(
                eventId: new EventId(LoggingEventIds.CollectionRuleCancelled, "CollectionRuleCancelled"),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_CollectionRuleCancelled);

        private static readonly Action<ILogger, int, Exception> _diagnosticRequestFailed =
            LoggerMessage.Define<int>(
                eventId: new EventId(LoggingEventIds.DiagnosticRequestFailed, "DiagnosticRequestFailed"),
                logLevel: LogLevel.Debug,
                formatString: Strings.LogFormatString_DiagnosticRequestFailed);

        private static readonly Action<ILogger, string, string, Exception> _invalidToken =
            LoggerMessage.Define<string, string>(
                eventId: new EventId(LoggingEventIds.InvalidTokenReference, "InvalidToken"),
                logLevel: LogLevel.Error,
                formatString: Strings.LogFormatString_InvalidToken);

        private static readonly Action<ILogger, string, Exception> _invalidActionReference =
            LoggerMessage.Define<string>(
                eventId: new EventId(LoggingEventIds.InvalidActionReference, "InvalidActionReference"),
                logLevel: LogLevel.Error,
                formatString: Strings.LogFormatString_InvalidActionReference);

        private static readonly Action<ILogger, string, Exception> _invalidResultReference =
            LoggerMessage.Define<string>(
            eventId: new EventId(LoggingEventIds.InvalidResultReference, "InvalidResultReference"),
            logLevel: LogLevel.Error,
            formatString: Strings.LogFormatString_InvalidResultReference);

        private static readonly Action<ILogger, string, Exception> _invalidSettings =
            LoggerMessage.Define<string>(
            eventId: new EventId(LoggingEventIds.InvalidSettings, "InvalidSettings"),
            logLevel: LogLevel.Error,
            formatString: Strings.LogFormatString_InvalidSettings);

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
                _apiKeyValidationFailure(logger, ConfigurationKeys.MonitorApiKey, error.ErrorMessage, null);
            }
        }

        public static void LogTempKey(this ILogger logger, string monitorApiKey)
        {
            _logTempKey(logger, Environment.NewLine, HeaderNames.Authorization, Monitoring.WebApi.AuthConstants.ApiKeySchema, monitorApiKey, null);
        }

        public static void DuplicateEgressProviderIgnored(this ILogger logger, string providerName, string providerType, string existingProviderType)
        {
            _duplicateEgressProviderIgnored(logger, providerName, providerType, existingProviderType, null);
        }

        public static void ApiKeyAuthenticationOptionsValidated(this ILogger logger)
        {
            _apiKeyAuthenticationOptionsValidated(logger, ConfigurationKeys.MonitorApiKey, null);
        }

        public static void NotifyPrivateKey(this ILogger logger, string fieldName)
        {
            _notifyPrivateKey(logger, fieldName, null);
        }

        public static void DuplicateCollectionRuleActionIgnored(this ILogger logger, string actionType)
        {
            _duplicateCollectionRuleActionIgnored(logger, actionType, null);
        }

        public static void DuplicateCollectionRuleTriggerIgnored(this ILogger logger, string triggerType)
        {
            _duplicateCollectionRuleTriggerIgnored(logger, triggerType, null);
        }

        public static void CollectionRuleStarted(this ILogger logger, string ruleName)
        {
            _collectionRuleStarted(logger, ruleName, null);
        }

        public static void CollectionRuleFailed(this ILogger logger, string ruleName, Exception ex)
        {
            _collectionRuleFailed(logger, ruleName, ex);
        }

        public static void CollectionRuleCompleted(this ILogger logger, string ruleName)
        {
            _collectionRuleCompleted(logger, ruleName, null);
        }

        public static void CollectionRulesStarted(this ILogger logger)
        {
            _collectionRulesStarted(logger, null);
        }

        public static void CollectionRuleActionStarted(this ILogger logger, string ruleName, string actionType)
        {
            _collectionRuleActionStarted(logger, ruleName, actionType, null);
        }

        public static void CollectionRuleActionCompleted(this ILogger logger, string ruleName, string actionType)
        {
            _collectionRuleActionCompleted(logger, ruleName, actionType, null);
        }

        public static void CollectionRuleTriggerStarted(this ILogger logger, string ruleName, string triggerType)
        {
            _collectionRuleTriggerStarted(logger, ruleName, triggerType, null);
        }

        public static void CollectionRuleTriggerCompleted(this ILogger logger, string ruleName, string triggerType)
        {
            _collectionRuleTriggerCompleted(logger, ruleName, triggerType, null);
        }

        public static void CollectionRuleThrottled(this ILogger logger, string ruleName)
        {
            _collectionRuleActionsThrottled(logger, ruleName, null);
        }

        public static void CollectionRuleActionFailed(this ILogger logger, string ruleName, string actionType, Exception ex)
        {
            _collectionRuleActionFailed(logger, ruleName, actionType, ex);
        }

        public static void CollectionRuleActionsCompleted(this ILogger logger, string ruleName)
        {
            _collectionRuleActionsCompleted(logger, ruleName, null);
        }

        public static void CollectionRulesStarting(this ILogger logger)
        {
            _collectionRulesStarting(logger, null);
        }

        public static void DiagnosticRequestCancelled(this ILogger logger, int processId)
        {
            _diagnosticRequestCancelled(logger, processId, null);
        }

        public static void CollectionRuleUnmatchedFilters(this ILogger logger, string ruleName)
        {
            _collectionRuleUnmatchedFilters(logger, ruleName, null);
        }

        public static void CollectionRuleConfigurationChanged(this ILogger logger)
        {
            _collectionRuleConfigurationChanged(logger, null);
        }

        public static void CollectionRulesStopping(this ILogger logger)
        {
            _collectionRulesStopping(logger, null);
        }

        public static void CollectionRulesStopped(this ILogger logger)
        {
            _collectionRulesStopped(logger, null);
        }

        public static void CollectionRuleCancelled(this ILogger logger, string ruleName)
        {
            _collectionRuleCancelled(logger, ruleName, null);
        }

        public static void DiagnosticRequestFailed(this ILogger logger, int processId, Exception ex)
        {
            _diagnosticRequestFailed(logger, processId, ex);
        }

        public static void InvalidTokenReference(this ILogger logger, string actionName, string setting)
        {
            _invalidToken(logger, actionName, setting, null);
        }

        public static void InvalidActionReference(this ILogger logger, string actionReference)
        {
            _invalidActionReference(logger, actionReference, null);
        }

        public static void InvalidResultReference(this ILogger logger, string actionResultToken)
        {
            _invalidResultReference(logger, actionResultToken, null);
        }

        public static void InvalidSettings(this ILogger logger, string settingsType)
        {
            _invalidSettings(logger, settingsType, null);
        }
    }
}
