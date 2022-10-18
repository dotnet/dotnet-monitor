﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        EgressProvideUnableToFindPropertyKey = 10,
        EgressProviderInvokeStreamAction = 11,
        EgressProviderSavedStream = 12,
        NoAuthentication = 13,
        InsecureAutheticationConfiguration = 14,
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
        MonitorApiKeyNotConfigured = 56,
        QueueDoesNotExist = 57,
        QueueOptionsPartiallySet = 58,
        WritingMessageToQueueFailed = 59,
        ExperienceSurvey = 60,
        DiagnosticPortNotInListenModeForCollectionRules = 61,
        ExtensionProbeStart = 62,
        ExtensionProbeRepo = 63,
        ExtensionProbeSucceeded = 64,
        ExtensionProbeFailed = 65,
        ExtensionStarting = 66,
        ExtensionConfigured = 67,
        ExtensionEgressPayloadCompleted = 68,
        ExtensionExited = 69,
        ExtensionOutputMessage = 70,
        ExtensionErrorMessage = 71,
        ExtensionNotOfType = 72,
        ExtensionDeclarationFileBroken = 73,
        ExtensionProgramMissing = 74,
        ExtensionMalformedOutput = 75,
    }

    internal static class LoggingEventIdsExtensions
    {
        public static EventId EventId(this LoggingEventIds enumVal)
        {
            string name = Enum.GetName(typeof(LoggingEventIds), enumVal);
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
