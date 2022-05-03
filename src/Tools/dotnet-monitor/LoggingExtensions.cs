﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.Monitor.Egress.AzureBlob;
using Microsoft.Diagnostics.Tools.Monitor.Extensibility;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal static class LoggingExtensions
    {
        private static readonly Action<ILogger, string, Exception> _egressProviderInvalidOptions =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.EgressProviderInvalidOptions.EventId(),
                logLevel: LogLevel.Error,
                formatString: Strings.LogFormatString_EgressProviderInvalidOptions);

        private static readonly Action<ILogger, int, Exception> _egressCopyActionStreamToEgressStream =
            LoggerMessage.Define<int>(
                eventId: LoggingEventIds.EgressCopyActionStreamToEgressStream.EventId(),
                logLevel: LogLevel.Debug,
                formatString: Strings.LogFormatString_EgressCopyActionStreamToEgressStream);

        private static readonly Action<ILogger, string, string, Exception> _egressProviderOptionsValidationFailure =
            LoggerMessage.Define<string, string>(
                eventId: LoggingEventIds.EgressProviderOptionsValidationFailure.EventId(),
                logLevel: LogLevel.Error,
                formatString: Strings.LogFormatString_EgressProviderOptionsValidationError);

        private static readonly Action<ILogger, string, string, Exception> _egressProviderUnableToFindPropertyKey =
            LoggerMessage.Define<string, string>(
                eventId: LoggingEventIds.EgressProvideUnableToFindPropertyKey.EventId(),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_EgressProvideUnableToFindPropertyKey);

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

        private static readonly Action<ILogger, Exception> _noAuthentication =
            LoggerMessage.Define(
                eventId: LoggingEventIds.NoAuthentication.EventId(),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_NoAuthentication);

        private static readonly Action<ILogger, Exception> _insecureAuthenticationConfiguration =
            LoggerMessage.Define(
                eventId: LoggingEventIds.InsecureAutheticationConfiguration.EventId(),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_InsecureAutheticationConfiguration);

        private static readonly Action<ILogger, string, Exception> _unableToListenToAddress =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.UnableToListenToAddress.EventId(),
                logLevel: LogLevel.Error,
                formatString: Strings.LogFormatString_UnableToListenToAddress);

        private static readonly Action<ILogger, string, Exception> _boundDefaultAddress =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.BoundDefaultAddress.EventId(),
                logLevel: LogLevel.Debug,
                formatString: Strings.LogFormatString_BoundDefaultAddress);

        private static readonly Action<ILogger, string, Exception> _boundMetricsAddress =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.BoundMetricsAddress.EventId(),
                logLevel: LogLevel.Debug,
                formatString: Strings.LogFormatString_BoundMetricsAddress);

        private static readonly Action<ILogger, string, Exception> _optionsValidationFalure =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.OptionsValidationFailure.EventId(),
                logLevel: LogLevel.Critical,
                formatString: Strings.LogFormatString_OptionsValidationFailure);

        private static readonly Action<ILogger, Exception> _runningElevated =
            LoggerMessage.Define(
                eventId: LoggingEventIds.RunningElevated.EventId(),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_RunningElevated);

        private static readonly Action<ILogger, Exception> _disabledNegotiateWhileElevated =
            LoggerMessage.Define(
                eventId: LoggingEventIds.DisabledNegotiateWhileElevated.EventId(),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_DisabledNegotiateWhileElevated);

        private static readonly Action<ILogger, string, string, Exception> _apiKeyValidationFailure =
            LoggerMessage.Define<string, string>(
                eventId: LoggingEventIds.ApiKeyValidationFailure.EventId(),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_ApiKeyValidationFailure);

        private static readonly Action<ILogger, string, string, string, string, Exception> _logTempKey =
            LoggerMessage.Define<string, string, string, string>(
                eventId: LoggingEventIds.LogTempApiKey.EventId(),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_LogTempApiKey);

        private static readonly Action<ILogger, string, string, string, Exception> _duplicateEgressProviderIgnored =
            LoggerMessage.Define<string, string, string>(
                eventId: LoggingEventIds.DuplicateEgressProviderIgnored.EventId(),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_DuplicateEgressProviderIgnored);

        private static readonly Action<ILogger, string, Exception> _apiKeyAuthenticationOptionsValidated =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.ApiKeyAuthenticationOptionsValidated.EventId(),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_ApiKeyAuthenticationOptionsValidated);

        private static readonly Action<ILogger, string, Exception> _notifyPrivateKey =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.NotifyPrivateKey.EventId(),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_NotifyPrivateKey);

        private static readonly Action<ILogger, string, Exception> _duplicateCollectionRuleActionIgnored =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.DuplicateCollectionRuleActionIgnored.EventId(),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_DuplicateCollectionRuleActionIgnored);

        private static readonly Action<ILogger, string, Exception> _duplicateCollectionRuleTriggerIgnored =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.DuplicateCollectionRuleTriggerIgnored.EventId(),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_DuplicateCollectionRuleTriggerIgnored);

        private static readonly Action<ILogger, string, Exception> _collectionRuleStarted =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.CollectionRuleStarted.EventId(),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_CollectionRuleStarted);

        private static readonly Action<ILogger, string, Exception> _collectionRuleFailed =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.CollectionRuleFailed.EventId(),
                logLevel: LogLevel.Error,
                formatString: Strings.LogFormatString_CollectionRuleFailed);

        private static readonly Action<ILogger, string, Exception> _collectionRuleCompleted =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.CollectionRuleCompleted.EventId(),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_CollectionRuleCompleted);

        private static readonly Action<ILogger, Exception> _collectionRulesStarted =
            LoggerMessage.Define(
                eventId: LoggingEventIds.CollectionRulesStarted.EventId(),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_CollectionRulesStarted);

        private static readonly Action<ILogger, string, string, Exception> _collectionRuleActionStarted =
            LoggerMessage.Define<string, string>(
                eventId: LoggingEventIds.CollectionRuleActionStarted.EventId(),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_CollectionRuleActionStarted);

        private static readonly Action<ILogger, string, string, Exception> _collectionRuleActionCompleted =
            LoggerMessage.Define<string, string>(
                eventId: LoggingEventIds.CollectionRuleActionCompleted.EventId(),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_CollectionRuleActionCompleted);

        private static readonly Action<ILogger, string, string, Exception> _collectionRuleTriggerStarted =
            LoggerMessage.Define<string, string>(
                eventId: LoggingEventIds.CollectionRuleTriggerStarted.EventId(),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_CollectionRuleTriggerStarted);

        private static readonly Action<ILogger, string, string, Exception> _collectionRuleTriggerCompleted =
            LoggerMessage.Define<string, string>(
                eventId: LoggingEventIds.CollectionRuleTriggerCompleted.EventId(),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_CollectionRuleTriggerCompleted);

        private static readonly Action<ILogger, string, Exception> _collectionRuleActionsThrottled =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.CollectionRuleActionsThrottled.EventId(),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_CollectionRuleActionsThrottled);

        private static readonly Action<ILogger, string, string, Exception> _collectionRuleActionFailed =
            LoggerMessage.Define<string, string>(
                eventId: LoggingEventIds.CollectionRuleActionFailed.EventId(),
                logLevel: LogLevel.Error,
                formatString: Strings.LogFormatString_CollectionRuleActionFailed);

        private static readonly Action<ILogger, string, Exception> _collectionRuleActionsCompleted =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.CollectionRuleActionsCompleted.EventId(),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_CollectionRuleActionsCompleted);

        private static readonly Action<ILogger, Exception> _collectionRulesStarting =
            LoggerMessage.Define(
                eventId: LoggingEventIds.CollectionRulesStarting.EventId(),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_CollectionRulesStarting);

        private static readonly Action<ILogger, int, Exception> _diagnosticRequestCancelled =
            LoggerMessage.Define<int>(
                eventId: LoggingEventIds.DiagnosticRequestCancelled.EventId(),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_DiagnosticRequestCancelled);

        private static readonly Action<ILogger, string, Exception> _collectionRuleUnmatchedFilters =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.CollectionRuleUnmatchedFilters.EventId(),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_CollectionRuleUnmatchedFilters);

        private static readonly Action<ILogger, Exception> _collectionRuleConfigurationChanged =
            LoggerMessage.Define(
                eventId: LoggingEventIds.CollectionRuleConfigurationChanged.EventId(),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_CollectionRuleConfigurationChanged);

        private static readonly Action<ILogger, Exception> _collectionRulesStopping =
            LoggerMessage.Define(
                eventId: LoggingEventIds.CollectionRulesStopping.EventId(),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_CollectionRulesStopping);

        private static readonly Action<ILogger, Exception> _collectionRulesStopped =
            LoggerMessage.Define(
                eventId: LoggingEventIds.CollectionRulesStopped.EventId(),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_CollectionRulesStopped);

        private static readonly Action<ILogger, string, Exception> _collectionRuleCancelled =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.CollectionRuleCancelled.EventId(),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_CollectionRuleCancelled);

        private static readonly Action<ILogger, int, Exception> _diagnosticRequestFailed =
            LoggerMessage.Define<int>(
                eventId: LoggingEventIds.DiagnosticRequestFailed.EventId(),
                logLevel: LogLevel.Debug,
                formatString: Strings.LogFormatString_DiagnosticRequestFailed);

        private static readonly Action<ILogger, string, string, Exception> _invalidActionReferenceToken =
            LoggerMessage.Define<string, string>(
                eventId: LoggingEventIds.InvalidActionReferenceToken.EventId(),
                logLevel: LogLevel.Error,
                formatString: Strings.LogFormatString_InvalidToken);

        private static readonly Action<ILogger, string, Exception> _invalidActionReference =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.InvalidActionReference.EventId(),
                logLevel: LogLevel.Error,
                formatString: Strings.LogFormatString_InvalidActionReference);

        private static readonly Action<ILogger, string, Exception> _invalidActionResultReference =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.InvalidActionResultReference.EventId(),
                logLevel: LogLevel.Error,
                formatString: Strings.LogFormatString_InvalidActionResultReference);

        private static readonly Action<ILogger, string, Exception> _actionSettingsTokenizationNotSupported =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.ActionSettingsTokenizationNotSupported.EventId(),
                logLevel: LogLevel.Error,
                formatString: Strings.LogFormatString_ActionSettingsTokenizationNotSupported);

        private static readonly Action<ILogger, string, Exception> _endpointTimeout =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.EndpointTimeout.EventId(),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_EndpointTimeout);

        private static readonly Action<ILogger, Guid, string, int, Exception> _loadingProfiler =
            LoggerMessage.Define<Guid, string, int>(
                eventId: LoggingEventIds.LoadingProfiler.EventId(),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_LoadingProfiler);

        private static readonly Action<ILogger, string, int, Exception> _setEnvironmentVariable =
            LoggerMessage.Define<string, int>(
                eventId: LoggingEventIds.SetEnvironmentVariable.EventId(),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_SetEnvironmentVariable);

        private static readonly Action<ILogger, string, int, Exception> _getEnvironmentVariable =
            LoggerMessage.Define<string, int>(
                eventId: LoggingEventIds.GetEnvironmentVariable.EventId(),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_GetEnvironmentVariable);

        private static readonly Action<ILogger, string, Exception> _monitorApiKeyNotConfigured =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.MonitorApiKeyNotConfigured.EventId(),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_ApiKeyNotConfigured);

        private static readonly Action<ILogger, string, string, string, Exception> _queueDoesNotExist =
            LoggerMessage.Define<string, string, string>(
                eventId: LoggingEventIds.QueueDoesNotExist.EventId(),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_QueueDoesNotExist);

        private static readonly Action<ILogger, string, string, Exception> _queueOptionsPartiallySet =
            LoggerMessage.Define<string, string>(
                eventId: LoggingEventIds.QueueOptionsPartiallySet.EventId(),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_QueueOptionsPartiallySet);

        private static readonly Action<ILogger, string, Exception> _writingMessageToQueueFailed =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.WritingMessageToQueueFailed.EventId(),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_WritingMessageToQueueFailed);

        private static readonly Action<ILogger, string, Exception> _experienceSurvey =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.ExperienceSurvey.EventId(),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_ExperienceSurvey);

        private static readonly Action<ILogger, string, Exception> _extensionProbeStart =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.ExtensionProbeStart.EventId(),
                logLevel: LogLevel.Debug,
                formatString: Strings.LogFormatString_ExtensionProbeStart);

        private static readonly Action<ILogger, string, string, Exception> _extensionProbeRepo =
            LoggerMessage.Define<string, string>(
                eventId: LoggingEventIds.ExtensionProbeRepo.EventId(),
                logLevel: LogLevel.Debug,
                formatString: Strings.LogFormatString_ExtensionProbeRepo);

        private static readonly Action<ILogger, string, string, Exception> _extensionProbeSucceeded =
            LoggerMessage.Define<string, string>(
                eventId: LoggingEventIds.ExtensionProbeSucceeded.EventId(),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_ExtensionProbeSucceeded);

        private static readonly Action<ILogger, string, Exception> _extensionProbeFailed =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.ExtensionProbeFailed.EventId(),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_ExtensionProbeFailed);

        private static readonly Action<ILogger, string, string, Exception> _extensionStarting =
            LoggerMessage.Define<string, string>(
                eventId: LoggingEventIds.ExtensionStarting.EventId(),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_ExtensionStarting);

        private static readonly Action<ILogger, string, int, Exception> _extensionConfigured =
            LoggerMessage.Define<string, int>(
                eventId: LoggingEventIds.ExtensionConfigured.EventId(),
                logLevel: LogLevel.Debug,
                formatString: Strings.LogFormatString_ExtensionConfigured);

        private static readonly Action<ILogger, int, Exception> _extensionEgressPayloadCompleted =
            LoggerMessage.Define<int>(
                eventId: LoggingEventIds.ExtensionEgressPayloadCompleted.EventId(),
                logLevel: LogLevel.Debug,
                formatString: Strings.LogFormatString_ExtensionEgressPayloadCompleted);

        private static readonly Action<ILogger, int, int, Exception> _extensionExited =
            LoggerMessage.Define<int, int>(
                eventId: LoggingEventIds.ExtensionExited.EventId(),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_ExtensionExited);

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
                _apiKeyValidationFailure(logger, ExtensionTypes.MonitorApiKey, error.ErrorMessage, null);
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
            _apiKeyAuthenticationOptionsValidated(logger, ExtensionTypes.MonitorApiKey, null);
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

        public static void InvalidActionReferenceToken(this ILogger logger, string actionName, string setting)
        {
            _invalidActionReferenceToken(logger, actionName, setting, null);
        }

        public static void InvalidActionReference(this ILogger logger, string actionReference)
        {
            _invalidActionReference(logger, actionReference, null);
        }

        public static void InvalidActionResultReference(this ILogger logger, string actionResultToken)
        {
            _invalidActionResultReference(logger, actionResultToken, null);
        }

        public static void ActionSettingsTokenizationNotSupported(this ILogger logger, string settingsType)
        {
            _actionSettingsTokenizationNotSupported(logger, settingsType, null);
        }

        public static void EndpointTimeout(this ILogger logger, string processId)
        {
            _endpointTimeout(logger, processId, null);
        }

        public static void LoadingProfiler(this ILogger logger, Guid profilerGuid, string path, int processId)
        {
            _loadingProfiler(logger, profilerGuid, path, processId, null);
        }

        public static void SettingEnvironmentVariable(this ILogger logger, string variableName, int processId)
        {
            _setEnvironmentVariable(logger, variableName, processId, null);
        }

        public static void GettingEnvironmentVariable(this ILogger logger, string variableName, int processId)
        {
            _getEnvironmentVariable(logger, variableName, processId, null);
        }

        public static void MonitorApiKeyNotConfigured(this ILogger logger)
        {
            const long myFwLinkId = 2187444;
            string fwLink = GetFwLinkWithCurrentLcidUri(myFwLinkId);
            _monitorApiKeyNotConfigured(logger, fwLink, null);
        }

        private static string GetFwLinkWithCurrentLcidUri(long fwlinkId)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                @"https://go.microsoft.com/fwlink/?linkid={0}&clcid=0x{1:x}",
                fwlinkId,
                CultureInfo.CurrentUICulture.LCID);
        }

        public static void QueueDoesNotExist(this ILogger logger, string queueName)
        {
            _queueDoesNotExist(logger, queueName, nameof(AzureBlobEgressProviderOptions.QueueName), nameof(AzureBlobEgressProviderOptions.QueueAccountUri), null);
        }

        public static void QueueOptionsPartiallySet(this ILogger logger)
        {
            _queueOptionsPartiallySet(logger, nameof(AzureBlobEgressProviderOptions.QueueName), nameof(AzureBlobEgressProviderOptions.QueueAccountUri), null);
        }

        public static void WritingMessageToQueueFailed(this ILogger logger, string queueName, Exception ex)
        {
            _writingMessageToQueueFailed(logger, queueName, ex);
        }

        public static void ExperienceSurvey(this ILogger logger)
        {
            _experienceSurvey(logger, Monitor.ExperienceSurvey.ExperienceSurveyLink, null);
        }

        public static void ExtensionProbeStart(this ILogger logger, string extensionMoniker)
        {
            _extensionProbeStart(logger, extensionMoniker, null);
        }

        public static void ExtensionProbeRepo(this ILogger logger, string extensionMoniker, IExtensionRepository extensionRepository)
        {
            _extensionProbeRepo(logger, extensionMoniker, extensionRepository.Name, null);
        }

        public static void ExtensionProbeSucceeded(this ILogger logger, string extensionMoniker, IExtension extension)
        {
            _extensionProbeSucceeded(logger, extensionMoniker, extension.Name, null);
        }

        public static void ExtensionProbeFailed(this ILogger logger, string extensionMoniker)
        {
            _extensionProbeFailed(logger, extensionMoniker, null);
        }

        public static void ExtensionStarting(this ILogger logger, string extensionPath, string arguments)
        {
            _extensionStarting(logger, extensionPath, arguments, null);
        }

        public static void ExtensionConfigured(this ILogger logger, string extensionPath, int pid)
        {
            _extensionConfigured(logger, extensionPath, pid, null);
        }

        public static void ExtensionEgressPayloadCompleted(this ILogger logger, int pid)
        {
            _extensionEgressPayloadCompleted(logger, pid, null);
        }

        public static void ExtensionExited(this ILogger logger, int pid, int exitCode)
        {
            _extensionExited(logger, pid, exitCode, null);
        }
    }
}
