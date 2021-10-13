// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Diagnostics.Tools.Monitor
{
    // The existing EventIds must not be duplicated, reused, or repurposed.
    // New logging events must use the next available EventId.
    internal static class LoggingEventIds
    {
        public const int EgressProviderAdded = 1;
        public const int EgressProviderInvalidOptions = 2;
        public const int EgressProviderInvalidType = 3;
        public const int EgressProviderValidatingOptions = 4;
        public const int EgressCopyActionStreamToEgressStream = 5;
        public const int EgressProviderOptionsValidationFailure = 6;
        public const int EgressProviderOptionValue = 7;
        public const int EgressStreamOptionValue = 8;
        public const int EgressProviderFileName = 9;
        public const int EgressProvideUnableToFindPropertyKey = 10;
        public const int EgressProviderInvokeStreamAction = 11;
        public const int EgressProviderSavedStream = 12;
        public const int NoAuthentication = 13;
        public const int InsecureAutheticationConfiguration = 14;
        public const int UnableToListenToAddress = 15;
        public const int BoundDefaultAddress = 16;
        public const int BoundMetricsAddress = 17;
        public const int OptionsValidationFailure = 18;
        public const int RunningElevated = 19;
        public const int DisabledNegotiateWhileElevated = 20;
        public const int ApiKeyValidationFailure = 21;
        public const int ApiKeyAuthenticationOptionsChanged = 22;
        public const int LogTempApiKey = 23;
        public const int DuplicateEgressProviderIgnored = 24;
        public const int ApiKeyAuthenticationOptionsValidated = 25;
        public const int NotifyPrivateKey = 26;
        public const int DuplicateCollectionRuleActionIgnored = 27;
        public const int DuplicateCollectionRuleTriggerIgnored = 28;
        public const int CollectionRuleStarted = 29;
        public const int CollectionRuleFailed = 30;
        public const int CollectionRuleCompleted = 31;
        public const int CollectionRulesStarted = 32;
        public const int CollectionRuleActionStarted = 33;
        public const int CollectionRuleActionCompleted = 34;
        public const int CollectionRuleTriggerStarted = 35;
        public const int CollectionRuleTriggerCompleted = 36;
        public const int CollectionRuleActionsThrottled = 37;
        public const int CollectionRuleActionFailed = 38;
        public const int CollectionRuleActionsCompleted = 39;
        public const int CollectionRulesStarting = 40;
        public const int DiagnosticRequestCancelled = 41;
        public const int CollectionRuleUnmatchedFilters = 42;
        public const int CollectionRuleConfigurationChanged = 43;
        public const int CollectionRulesStopping = 44;
        public const int CollectionRulesStopped = 45;
        public const int CollectionRuleCancelled = 46;
        public const int DiagnosticRequestFailed = 47;
        public const int InvalidToken = 48;
        public const int InvalidActionReference = 49;
        public const int InvalidResultReference = 50;
    }
}
