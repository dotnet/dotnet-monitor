// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.Extensions.Logging;
using System;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    // The existing EventIds must not be duplicated, reused, or repurposed.
    // New logging events must use the next available EventId.
    internal enum LoggingEventIds
    {
        EgressProviderAdded = 1,
        EgressProviderInvalidOptions = 2,
        EgressProviderInvalidType = 3,
        EgressProviderValidatingOptions = 4,
        EgressCopyActionStreamToEgressStream = 5,
        EgressProviderOptionsValidationFailure = 6,
        EgressProviderOptionValue = 7,
        EgressStreamOptionValue = 8,
        EgressProviderFileName = 9,
        EgressProvideUnableToFindPropertyKey = 10, // Moved to Azure extension
        EgressProviderInvokeStreamAction = 11,
        EgressProviderSavedStream = 12,
        NoAuthentication = 13,
        InsecureAuthenticationConfiguration = 14,
        UnableToListenToAddress = 15,
        BoundDefaultAddress = 16,
        BoundMetricsAddress = 17,
        OptionsValidationFailure = 18,
        RunningElevated = 19,
        DisabledNegotiateWhileElevated = 20,
        ApiKeyValidationFailure = 21,
        ApiKeyAuthenticationOptionsChanged = 22,
        LogTempApiKey = 23,
        DuplicateEgressProviderIgnored = 24,
        ApiKeyAuthenticationOptionsValidated = 25,
        NotifyPrivateKey = 26,
        DuplicateCollectionRuleActionIgnored = 27,
        DuplicateCollectionRuleTriggerIgnored = 28,
        CollectionRuleStarted = 29,
        CollectionRuleFailed = 30,
        CollectionRuleCompleted = 31,
        CollectionRulesStarted = 32,
        CollectionRuleActionStarted = 33,
        CollectionRuleActionCompleted = 34,
        CollectionRuleTriggerStarted = 35,
        CollectionRuleTriggerCompleted = 36,
        CollectionRuleActionsThrottled = 37,
        CollectionRuleActionFailed = 38,
        CollectionRuleActionsCompleted = 39,
        CollectionRulesStarting = 40,
        DiagnosticRequestCancelled = 41,
        CollectionRuleUnmatchedFilters = 42,
        CollectionRuleConfigurationChanged = 43,
        CollectionRulesStopping = 44,
        CollectionRulesStopped = 45,
        CollectionRuleCancelled = 46,
        DiagnosticRequestFailed = 47,
        InvalidActionReferenceToken = 48,
        InvalidActionReference = 49,
        InvalidActionResultReference = 50,
        ActionSettingsTokenizationNotSupported = 51,
        EndpointTimeout = 52,
        LoadingProfiler = 53,
        SetEnvironmentVariable = 54,
        GetEnvironmentVariable = 55,
        MonitorApiKeyNotConfigured = 56, // Moved to Azure extension
        QueueDoesNotExist = 57, // Moved to Azure extension
        QueueOptionsPartiallySet = 58, // Moved to Azure extension
        WritingMessageToQueueFailed = 59, // Moved to Azure extension
        ExperienceSurvey = 60,
        DiagnosticPortNotInListenModeForCollectionRules = 61,
        RuntimeInstanceCookieFailedToFilterSelf = 62,
        ParsingUrlFailed = 63,
        IntermediateFileDeletionFailed = 64,
        DiagnosticPortDeleteAttempt = 65,
        DiagnosticPortDeleteFailed = 66,
        DiagnosticPortAlteredWhileInUse = 67,
        DiagnosticPortWatchingFailed = 68,
        InvalidMetadata = 69, // Moved to Azure extension
        DuplicateKeyInMetadata = 70, // Moved to Azure extension
        EnvironmentVariableNotFound = 71, // Moved to Azure extension
        EnvironmentBlockNotSupported = 72, // Moved to Azure extension
        FailedInitializeSharedLibraryStorage = 73,
        UnableToApplyProfiler = 74,
        SharedLibraryPath = 75,
        ConnectionModeConnect = 76,
        ConnectionModeListen = 77,
        ExperimentalFeatureEnabled = 78,
        StartCollectArtifact = 79,
        StartupHookEnvironmentMissing = 80, // Unused
        StartupHookMissing = 81, // Unused
        StartupHookInstructions = 82,
        ExtensionProbeStart = 83,
        ExtensionProbeRepo = 84,
        ExtensionProbeSucceeded = 85,
        ExtensionProbeFailed = 86,
        ExtensionStarting = 87,
        ExtensionConfigured = 88,
        ExtensionEgressPayloadCompleted = 89,
        ExtensionExited = 90,
        ExtensionOutputMessage = 91,
        ExtensionErrorMessage = 92,
        ExtensionNotOfType = 93,
        ExtensionManifestNotParsable = 94,
        ExtensionMalformedOutput = 95,
        EgressProviderTypeNotExist = 96,
        ProfilerRuntimeIdentifier = 97,
        StartupHookApplyFailed = 98,
        EndpointInitializationFailed = 99,
        EndpointRemovalFailed = 100,
        WatchForStdinDisconnectFailure = 101,
        UnableToApplyHostingStartup = 102,
        UnableToApplyInProcessFeatureFlags = 103
    }

    internal static class LoggingEventIdsExtensions
    {
        public static EventId EventId(this LoggingEventIds enumVal)
        {
            string? name = Enum.GetName(typeof(LoggingEventIds), enumVal);
            int id = enumVal.Id();
            return new EventId(id, name);
        }
        public static int Id(this LoggingEventIds enumVal)
        {
            int id = (int)enumVal;
            return id;
        }
    }
}
