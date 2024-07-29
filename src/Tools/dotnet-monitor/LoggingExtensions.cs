// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        private static readonly Action<ILogger, string, Exception?> _egressProviderInvalidOptions =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.EgressProviderInvalidOptions.EventId(),
                logLevel: LogLevel.Error,
                formatString: Strings.LogFormatString_EgressProviderInvalidOptions);

        private static readonly Action<ILogger, int, Exception?> _egressCopyActionStreamToEgressStream =
            LoggerMessage.Define<int>(
                eventId: LoggingEventIds.EgressCopyActionStreamToEgressStream.EventId(),
                logLevel: LogLevel.Debug,
                formatString: Strings.LogFormatString_EgressCopyActionStreamToEgressStream);

        private static readonly Action<ILogger, string, string, Exception?> _egressProviderOptionsValidationFailure =
            LoggerMessage.Define<string, string>(
                eventId: LoggingEventIds.EgressProviderOptionsValidationFailure.EventId(),
                logLevel: LogLevel.Error,
                formatString: Strings.LogFormatString_EgressProviderOptionsValidationError);

        private static readonly Action<ILogger, string, Exception?> _egressProviderInvokeStreamAction =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.EgressProviderInvokeStreamAction.EventId(),
                logLevel: LogLevel.Debug,
                formatString: Strings.LogFormatString_EgressProviderInvokeStreamAction);

        private static readonly Action<ILogger, string, string, Exception?> _egressProviderSavedStream =
            LoggerMessage.Define<string, string>(
                eventId: LoggingEventIds.EgressProviderSavedStream.EventId(),
                logLevel: LogLevel.Debug,
                formatString: Strings.LogFormatString_EgressProviderSavedStream);

        private static readonly Action<ILogger, Exception?> _noAuthentication =
            LoggerMessage.Define(
                eventId: LoggingEventIds.NoAuthentication.EventId(),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_NoAuthentication);

        private static readonly Action<ILogger, Exception?> _insecureAuthenticationConfiguration =
            LoggerMessage.Define(
                eventId: LoggingEventIds.InsecureAuthenticationConfiguration.EventId(),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_InsecureAuthenticationConfiguration);

        private static readonly Action<ILogger, string, Exception?> _unableToListenToAddress =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.UnableToListenToAddress.EventId(),
                logLevel: LogLevel.Error,
                formatString: Strings.LogFormatString_UnableToListenToAddress);

        private static readonly Action<ILogger, string, Exception?> _boundDefaultAddress =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.BoundDefaultAddress.EventId(),
                logLevel: LogLevel.Debug,
                formatString: Strings.LogFormatString_BoundDefaultAddress);

        private static readonly Action<ILogger, string, Exception?> _boundMetricsAddress =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.BoundMetricsAddress.EventId(),
                logLevel: LogLevel.Debug,
                formatString: Strings.LogFormatString_BoundMetricsAddress);

        private static readonly Action<ILogger, string, Exception?> _optionsValidationFailure =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.OptionsValidationFailure.EventId(),
                logLevel: LogLevel.Critical,
                formatString: Strings.LogFormatString_OptionsValidationFailure);

        private static readonly Action<ILogger, Exception?> _runningElevated =
            LoggerMessage.Define(
                eventId: LoggingEventIds.RunningElevated.EventId(),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_RunningElevated);

        private static readonly Action<ILogger, Exception?> _disabledNegotiateWhileElevated =
            LoggerMessage.Define(
                eventId: LoggingEventIds.DisabledNegotiateWhileElevated.EventId(),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_DisabledNegotiateWhileElevated);

        private static readonly Action<ILogger, string, string, Exception?> _apiKeyValidationFailure =
            LoggerMessage.Define<string, string>(
                eventId: LoggingEventIds.ApiKeyValidationFailure.EventId(),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_ApiKeyValidationFailure);

        private static readonly Action<ILogger, string, string, string, string, Exception?> _logTempKey =
            LoggerMessage.Define<string, string, string, string>(
                eventId: LoggingEventIds.LogTempApiKey.EventId(),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_LogTempApiKey);

        private static readonly Action<ILogger, string, string, string, Exception?> _duplicateEgressProviderIgnored =
            LoggerMessage.Define<string, string, string>(
                eventId: LoggingEventIds.DuplicateEgressProviderIgnored.EventId(),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_DuplicateEgressProviderIgnored);

        private static readonly Action<ILogger, string, Exception?> _apiKeyAuthenticationOptionsValidated =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.ApiKeyAuthenticationOptionsValidated.EventId(),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_ApiKeyAuthenticationOptionsValidated);

        private static readonly Action<ILogger, string, Exception?> _notifyPrivateKey =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.NotifyPrivateKey.EventId(),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_NotifyPrivateKey);

        private static readonly Action<ILogger, string, Exception?> _duplicateCollectionRuleActionIgnored =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.DuplicateCollectionRuleActionIgnored.EventId(),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_DuplicateCollectionRuleActionIgnored);

        private static readonly Action<ILogger, string, Exception?> _duplicateCollectionRuleTriggerIgnored =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.DuplicateCollectionRuleTriggerIgnored.EventId(),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_DuplicateCollectionRuleTriggerIgnored);

        private static readonly Action<ILogger, string, Exception?> _collectionRuleStarted =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.CollectionRuleStarted.EventId(),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_CollectionRuleStarted);

        private static readonly Action<ILogger, string, Exception?> _collectionRuleFailed =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.CollectionRuleFailed.EventId(),
                logLevel: LogLevel.Error,
                formatString: Strings.LogFormatString_CollectionRuleFailed);

        private static readonly Action<ILogger, string, Exception?> _collectionRuleCompleted =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.CollectionRuleCompleted.EventId(),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_CollectionRuleCompleted);

        private static readonly Action<ILogger, Exception?> _collectionRulesStarted =
            LoggerMessage.Define(
                eventId: LoggingEventIds.CollectionRulesStarted.EventId(),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_CollectionRulesStarted);

        private static readonly Action<ILogger, string, string, Exception?> _collectionRuleActionStarted =
            LoggerMessage.Define<string, string>(
                eventId: LoggingEventIds.CollectionRuleActionStarted.EventId(),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_CollectionRuleActionStarted);

        private static readonly Action<ILogger, string, string, Exception?> _collectionRuleActionCompleted =
            LoggerMessage.Define<string, string>(
                eventId: LoggingEventIds.CollectionRuleActionCompleted.EventId(),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_CollectionRuleActionCompleted);

        private static readonly Action<ILogger, string, string, Exception?> _collectionRuleTriggerStarted =
            LoggerMessage.Define<string, string>(
                eventId: LoggingEventIds.CollectionRuleTriggerStarted.EventId(),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_CollectionRuleTriggerStarted);

        private static readonly Action<ILogger, string, string, Exception?> _collectionRuleTriggerCompleted =
            LoggerMessage.Define<string, string>(
                eventId: LoggingEventIds.CollectionRuleTriggerCompleted.EventId(),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_CollectionRuleTriggerCompleted);

        private static readonly Action<ILogger, string, Exception?> _collectionRuleActionsThrottled =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.CollectionRuleActionsThrottled.EventId(),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_CollectionRuleActionsThrottled);

        private static readonly Action<ILogger, string, string, Exception?> _collectionRuleActionFailed =
            LoggerMessage.Define<string, string>(
                eventId: LoggingEventIds.CollectionRuleActionFailed.EventId(),
                logLevel: LogLevel.Error,
                formatString: Strings.LogFormatString_CollectionRuleActionFailed);

        private static readonly Action<ILogger, string, Exception?> _collectionRuleActionsCompleted =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.CollectionRuleActionsCompleted.EventId(),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_CollectionRuleActionsCompleted);

        private static readonly Action<ILogger, Exception?> _collectionRulesStarting =
            LoggerMessage.Define(
                eventId: LoggingEventIds.CollectionRulesStarting.EventId(),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_CollectionRulesStarting);

        private static readonly Action<ILogger, int, Exception?> _diagnosticRequestCancelled =
            LoggerMessage.Define<int>(
                eventId: LoggingEventIds.DiagnosticRequestCancelled.EventId(),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_DiagnosticRequestCancelled);

        private static readonly Action<ILogger, string, Exception?> _collectionRuleUnmatchedFilters =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.CollectionRuleUnmatchedFilters.EventId(),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_CollectionRuleUnmatchedFilters);

        private static readonly Action<ILogger, Exception?> _collectionRuleConfigurationChanged =
            LoggerMessage.Define(
                eventId: LoggingEventIds.CollectionRuleConfigurationChanged.EventId(),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_CollectionRuleConfigurationChanged);

        private static readonly Action<ILogger, Exception?> _collectionRulesStopping =
            LoggerMessage.Define(
                eventId: LoggingEventIds.CollectionRulesStopping.EventId(),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_CollectionRulesStopping);

        private static readonly Action<ILogger, Exception?> _collectionRulesStopped =
            LoggerMessage.Define(
                eventId: LoggingEventIds.CollectionRulesStopped.EventId(),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_CollectionRulesStopped);

        private static readonly Action<ILogger, string, Exception?> _collectionRuleCancelled =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.CollectionRuleCancelled.EventId(),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_CollectionRuleCancelled);

        private static readonly Action<ILogger, int, Exception?> _diagnosticRequestFailed =
            LoggerMessage.Define<int>(
                eventId: LoggingEventIds.DiagnosticRequestFailed.EventId(),
                logLevel: LogLevel.Debug,
                formatString: Strings.LogFormatString_DiagnosticRequestFailed);

        private static readonly Action<ILogger, string, string, Exception?> _invalidActionReferenceToken =
            LoggerMessage.Define<string, string>(
                eventId: LoggingEventIds.InvalidActionReferenceToken.EventId(),
                logLevel: LogLevel.Error,
                formatString: Strings.LogFormatString_InvalidToken);

        private static readonly Action<ILogger, string, Exception?> _invalidActionReference =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.InvalidActionReference.EventId(),
                logLevel: LogLevel.Error,
                formatString: Strings.LogFormatString_InvalidActionReference);

        private static readonly Action<ILogger, string, Exception?> _invalidActionResultReference =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.InvalidActionResultReference.EventId(),
                logLevel: LogLevel.Error,
                formatString: Strings.LogFormatString_InvalidActionResultReference);

        private static readonly Action<ILogger, string, Exception?> _actionSettingsTokenizationNotSupported =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.ActionSettingsTokenizationNotSupported.EventId(),
                logLevel: LogLevel.Error,
                formatString: Strings.LogFormatString_ActionSettingsTokenizationNotSupported);

        private static readonly Action<ILogger, string, Exception?> _endpointTimeout =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.EndpointTimeout.EventId(),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_EndpointTimeout);

        private static readonly Action<ILogger, Guid, string, int, Exception?> _loadingProfiler =
            LoggerMessage.Define<Guid, string, int>(
                eventId: LoggingEventIds.LoadingProfiler.EventId(),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_LoadingProfiler);

        private static readonly Action<ILogger, string, int, Exception?> _setEnvironmentVariable =
            LoggerMessage.Define<string, int>(
                eventId: LoggingEventIds.SetEnvironmentVariable.EventId(),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_SetEnvironmentVariable);

        private static readonly Action<ILogger, string, int, Exception?> _getEnvironmentVariable =
            LoggerMessage.Define<string, int>(
                eventId: LoggingEventIds.GetEnvironmentVariable.EventId(),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_GetEnvironmentVariable);

        private static readonly Action<ILogger, string, Exception?> _monitorApiKeyNotConfigured =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.MonitorApiKeyNotConfigured.EventId(),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_ApiKeyNotConfigured);

        private static readonly Action<ILogger, string, Exception?> _experienceSurvey =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.ExperienceSurvey.EventId(),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_ExperienceSurvey);

        private static readonly Action<ILogger, Exception?> _diagnosticPortNotInListenModeForCollectionRules =
            LoggerMessage.Define(
                eventId: LoggingEventIds.DiagnosticPortNotInListenModeForCollectionRules.EventId(),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_DiagnosticPortNotInListenModeForCollectionRules);

        private static readonly Action<ILogger, string, Exception?> _extensionProbeStart =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.ExtensionProbeStart.EventId(),
                logLevel: LogLevel.Debug,
                formatString: Strings.LogFormatString_ExtensionProbeStart);

        private static readonly Action<ILogger, string, string, Exception?> _extensionProbeSucceeded =
            LoggerMessage.Define<string, string>(
                eventId: LoggingEventIds.ExtensionProbeSucceeded.EventId(),
                logLevel: LogLevel.Debug,
                formatString: Strings.LogFormatString_ExtensionProbeSucceeded);

        private static readonly Action<ILogger, string, Exception?> _extensionProbeFailed =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.ExtensionProbeFailed.EventId(),
                logLevel: LogLevel.Error,
                formatString: Strings.LogFormatString_ExtensionProbeFailed);

        private static readonly Action<ILogger, string, Exception?> _extensionStarting =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.ExtensionStarting.EventId(),
                logLevel: LogLevel.Debug,
                formatString: Strings.LogFormatString_ExtensionStarting);

        private static readonly Action<ILogger, string, int, Exception?> _extensionConfigured =
            LoggerMessage.Define<string, int>(
                eventId: LoggingEventIds.ExtensionConfigured.EventId(),
                logLevel: LogLevel.Debug,
                formatString: Strings.LogFormatString_ExtensionConfigured);

        private static readonly Action<ILogger, int, Exception?> _extensionEgressPayloadCompleted =
            LoggerMessage.Define<int>(
                eventId: LoggingEventIds.ExtensionEgressPayloadCompleted.EventId(),
                logLevel: LogLevel.Debug,
                formatString: Strings.LogFormatString_ExtensionEgressPayloadCompleted);

        private static readonly Action<ILogger, int, int, Exception?> _extensionExited =
            LoggerMessage.Define<int, int>(
                eventId: LoggingEventIds.ExtensionExited.EventId(),
                logLevel: LogLevel.Debug,
                formatString: Strings.LogFormatString_ExtensionExited);

        private static readonly Action<ILogger, int, string, Exception?> _extensionOutputMessage =
            LoggerMessage.Define<int, string>(
                eventId: LoggingEventIds.ExtensionOutputMessage.EventId(),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_ExtensionOutputMessage);

        private static readonly Action<ILogger, int, string, Exception?> _extensionErrorMessage =
            LoggerMessage.Define<int, string>(
                eventId: LoggingEventIds.ExtensionErrorMessage.EventId(),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_ExtensionErrorMessage);

        private static readonly Action<ILogger, string, string, string, Exception?> _extensionNotOfType =
            LoggerMessage.Define<string, string, string>(
                eventId: LoggingEventIds.ExtensionNotOfType.EventId(),
                logLevel: LogLevel.Error,
                formatString: Strings.LogFormatString_ExtensionNotOfType);

        private static readonly Action<ILogger, string, Exception> _extensionManifestNotParsable =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.ExtensionManifestNotParsable.EventId(),
                logLevel: LogLevel.Error,
                formatString: Strings.LogFormatString_ExtensionManifestNotParsable);

        private static readonly Action<ILogger, int, string, string, Exception?> _extensionMalformedOutput =
            LoggerMessage.Define<int, string, string>(
                eventId: LoggingEventIds.ExtensionMalformedOutput.EventId(),
                logLevel: LogLevel.Error,
                formatString: Strings.LogFormatString_ExtensionMalformedOutput);

        private static readonly Action<ILogger, Exception> _runtimeInstanceCookieFailedToFilterSelf =
            LoggerMessage.Define(
                eventId: LoggingEventIds.RuntimeInstanceCookieFailedToFilterSelf.EventId(),
                logLevel: LogLevel.Debug,
                formatString: Strings.LogFormatString_RuntimeInstanceCookieFailedToFilterSelf);

        private static readonly Action<ILogger, string, Exception> _parsingUrlFailed =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.ParsingUrlFailed.EventId(),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_ParsingUrlFailed);

        private static readonly Action<ILogger, string, Exception> _intermediateFileDeletionFailed =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.IntermediateFileDeletionFailed.EventId(),
                logLevel: LogLevel.Debug,
                formatString: Strings.LogFormatString_IntermediateFileDeletionFailed);

        private static readonly Action<ILogger, string, Exception?> _diagnosticPortDeleteAttempt =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.DiagnosticPortDeleteAttempt.EventId(),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_DiagnosticPortDeleteAttempt);

        private static readonly Action<ILogger, string, Exception> _diagnosticPortDeleteFailed =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.DiagnosticPortDeleteFailed.EventId(),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_DiagnosticPortDeleteFailed);

        private static readonly Action<ILogger, string, Exception?> _diagnosticPortAlteredWhileInUse =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.DiagnosticPortAlteredWhileInUse.EventId(),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_DiagnosticPortAlteredWhileInUse);

        private static readonly Action<ILogger, string, Exception> _diagnosticPortWatchingFailed =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.DiagnosticPortWatchingFailed.EventId(),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_DiagnosticPortWatchingFailed);

        private static readonly Action<ILogger, Exception> _failedInitializeSharedLibraryStorage =
            LoggerMessage.Define(
                eventId: LoggingEventIds.FailedInitializeSharedLibraryStorage.EventId(),
                logLevel: LogLevel.Error,
                formatString: Strings.LogFormatString_FailedInitializeSharedLibraryStorage);

        private static readonly Action<ILogger, Exception> _unableToApplyProfiler =
            LoggerMessage.Define(
                eventId: LoggingEventIds.UnableToApplyProfiler.EventId(),
                logLevel: LogLevel.Error,
                formatString: Strings.LogFormatString_UnableToApplyProfiler);

        private static readonly Action<ILogger, string, Exception?> _sharedlibraryPath =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.SharedLibraryPath.EventId(),
                logLevel: LogLevel.Debug,
                formatString: Strings.LogFormatString_SharedLibraryPath);

        private static readonly Action<ILogger, Exception?> _connectionModeConnect =
            LoggerMessage.Define(
                eventId: LoggingEventIds.ConnectionModeConnect.EventId(),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_ConnectionModeConnect);

        private static readonly Action<ILogger, string, Exception?> _connectionModeListen =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.ConnectionModeListen.EventId(),
                logLevel: LogLevel.Information,
                formatString: Strings.LogFormatString_ConnectionModeListen);

        private static readonly Action<ILogger, string, Exception?> _experimentalFeatureEnabled =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.ExperimentalFeatureEnabled.EventId(),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_ExperimentalFeatureEnabled);

        private static readonly Action<ILogger, string, Exception?> _startCollectArtifact =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.StartCollectArtifact.EventId(),
                logLevel: LogLevel.Debug,
                formatString: Strings.LogFormatString_StartCollectArtifact);

        private static readonly Action<ILogger, int, string, string, Exception?> _startupHookInstructions =
            LoggerMessage.Define<int, string, string>(
                eventId: LoggingEventIds.StartupHookInstructions.EventId(),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_StartupHookInstructions);

        private static readonly Action<ILogger, Exception> _unableToWatchForDisconnect =
            LoggerMessage.Define(
                eventId: LoggingEventIds.WatchForStdinDisconnectFailure.EventId(),
                logLevel: LogLevel.Error,
                formatString: Strings.LogFormatString_UnableToWatchForDisconnect);

        private static readonly Action<ILogger, string, Exception?> _egressProviderTypeNotExist =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.EgressProviderTypeNotExist.EventId(),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_EgressProviderTypeNotExist);

        private static readonly Action<ILogger, string, string, Exception?> _profilerRuntimeIdentifier =
            LoggerMessage.Define<string, string>(
                eventId: LoggingEventIds.ProfilerRuntimeIdentifier.EventId(),
                logLevel: LogLevel.Debug,
                formatString: Strings.LogFormatString_ProfilerRuntimeIdentifier);

        private static readonly Action<ILogger, string, Exception> _startupHookApplyFailed =
            LoggerMessage.Define<string>(
                eventId: LoggingEventIds.StartupHookApplyFailed.EventId(),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_StartupHookApplyFailed);

        private static readonly Action<ILogger, int, Exception> _endpointInitializationFailed =
            LoggerMessage.Define<int>(
                eventId: LoggingEventIds.EndpointInitializationFailed.EventId(),
                logLevel: LogLevel.Warning,
                formatString: Strings.LogFormatString_EndpointInitializationFailed);

        private static readonly Action<ILogger, int, Exception> _endpointRemovalFailed =
            LoggerMessage.Define<int>(
                eventId: LoggingEventIds.EndpointRemovalFailed.EventId(),
                logLevel: LogLevel.Debug,
                formatString: Strings.LogFormatString_EndpointRemovalFailed);

        private static readonly Action<ILogger, Exception> _unableToApplyInProcessFeatureFlags =
            LoggerMessage.Define(
                eventId: LoggingEventIds.UnableToApplyInProcessFeatureFlags.EventId(),
                logLevel: LogLevel.Error,
                formatString: Strings.LogFormatString_UnableToApplyInProcessFeatureFlags);

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
                _optionsValidationFailure(logger, failure, null);
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
#nullable disable
                _apiKeyValidationFailure(logger, ConfigurationKeys.MonitorApiKey, error.ErrorMessage, null);
#nullable restore
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

        public static void ExperienceSurvey(this ILogger logger)
        {
            _experienceSurvey(logger, Monitor.ExperienceSurvey.ExperienceSurveyLink, null);
        }

        public static void DiagnosticPortNotInListenModeForCollectionRules(this ILogger logger)
        {
            _diagnosticPortNotInListenModeForCollectionRules(logger, null);
        }

        public static void ExtensionProbeStart(this ILogger logger, string extensionName)
        {
            _extensionProbeStart(logger, extensionName, null);
        }

        public static void ExtensionProbeSucceeded(this ILogger logger, string extensionName, IExtension extension)
        {
            _extensionProbeSucceeded(logger, extensionName, extension.DisplayName, null);
        }

        public static void ExtensionProbeFailed(this ILogger logger, string extensionName)
        {
            _extensionProbeFailed(logger, extensionName, null);
        }

        public static void ExtensionStarting(this ILogger logger, string extensionName)
        {
            _extensionStarting(logger, extensionName, null);
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

        public static void ExtensionOutputMessage(this ILogger logger, int pid, string message)
        {
            _extensionOutputMessage(logger, pid, message, null);
        }

        public static void ExtensionErrorMessage(this ILogger logger, int pid, string message)
        {
            _extensionErrorMessage(logger, pid, message, null);
        }

        public static void ExtensionNotOfType(this ILogger logger, string extensionName, IExtension extension, Type desiredType)
        {
            _extensionNotOfType(logger, extensionName, extension.DisplayName, desiredType.Name, null);
        }

        public static void ExtensionManifestNotParsable(this ILogger logger, string manifestFile, Exception ex)
        {
            _extensionManifestNotParsable(logger, manifestFile, ex);
        }

        public static void ExtensionMalformedOutput(this ILogger logger, int pid, string message, Type resultType)
        {
            _extensionMalformedOutput(logger, pid, message, resultType.Name, null);
        }

        public static void RuntimeInstanceCookieFailedToFilterSelf(this ILogger logger, Exception ex)
        {
            _runtimeInstanceCookieFailedToFilterSelf(logger, ex);
        }

        public static void ParsingUrlFailed(this ILogger logger, string url, Exception ex)
        {
            _parsingUrlFailed(logger, url, ex);
        }

        public static void IntermediateFileDeletionFailed(this ILogger logger, string intermediateFilePath, Exception ex)
        {
            _intermediateFileDeletionFailed(logger, intermediateFilePath, ex);
        }

        public static void DiagnosticPortDeleteAttempt(this ILogger logger, string diagnosticPort)
        {
            _diagnosticPortDeleteAttempt(logger, diagnosticPort, null);
        }

        public static void DiagnosticPortDeleteFailed(this ILogger logger, string diagnosticPort, Exception ex)
        {
            _diagnosticPortDeleteFailed(logger, diagnosticPort, ex);
        }

        public static void DiagnosticPortAlteredWhileInUse(this ILogger logger, string diagnosticPort)
        {
            _diagnosticPortAlteredWhileInUse(logger, diagnosticPort, null);
        }

        public static void DiagnosticPortWatchingFailed(this ILogger logger, string diagnosticPort, Exception ex)
        {
            _diagnosticPortWatchingFailed(logger, diagnosticPort, ex);
        }

        public static void FailedInitializeSharedLibraryStorage(this ILogger logger, Exception ex)
        {
            _failedInitializeSharedLibraryStorage(logger, ex);
        }

        public static void UnableToApplyProfiler(this ILogger logger, Exception ex)
        {
            _unableToApplyProfiler(logger, ex);
        }

        public static void SharedLibraryPath(this ILogger logger, string path)
        {
            _sharedlibraryPath(logger, path, null);
        }

        public static void ConnectionModeConnect(this ILogger logger)
        {
            _connectionModeConnect(logger, null);
        }

        public static void ConnectionModeListen(this ILogger logger, string path)
        {
            _connectionModeListen(logger, path, null);
        }

        public static void ExperimentalFeatureEnabled(this ILogger logger, string name)
        {
            _experimentalFeatureEnabled(logger, name, null);
        }

        public static void StartCollectArtifact(this ILogger logger, string artifactType)
        {
            _startCollectArtifact(logger, artifactType, null);
        }

        public static void StartupHookInstructions(this ILogger logger, int processId, string startupHookFileName, string startupHookLibraryPath)
        {
            _startupHookInstructions(logger, processId, startupHookFileName, startupHookLibraryPath, null);
        }

        public static void UnableToWatchForDisconnect(this ILogger logger, Exception exception)
        {
            _unableToWatchForDisconnect(logger, exception);
        }

        public static void EgressProviderTypeNotExist(this ILogger logger, string providerType)
        {
            _egressProviderTypeNotExist(logger, providerType, null);
        }

        public static void ProfilerRuntimeIdentifier(this ILogger logger, string runtimeIdentifier, string source)
        {
            _profilerRuntimeIdentifier(logger, runtimeIdentifier, source, null);
        }

        public static void StartupHookApplyFailed(this ILogger logger, string startupHookFileName, Exception ex)
        {
            _startupHookApplyFailed(logger, startupHookFileName, ex);
        }

        public static void EndpointInitializationFailed(this ILogger logger, int processId, Exception ex)
        {
            _endpointInitializationFailed(logger, processId, ex);
        }

        public static void EndpointRemovalFailed(this ILogger logger, int processId, Exception ex)
        {
            _endpointRemovalFailed(logger, processId, ex);
        }

        public static void UnableToApplyInProcessFeatureFlags(this ILogger logger, Exception ex)
        {
            _unableToApplyInProcessFeatureFlags(logger, ex);
        }
    }
}
