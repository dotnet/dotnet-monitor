// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers.EventCounterShortcuts;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Triggers;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Configuration
{
    internal static class CollectionRuleBindingHelper
    {
        public static object CreateActionSettings(
            string actionType,
            Func<CollectDumpOptions> collectDump,
            Func<CollectExceptionsOptions> collectExceptions
        ) => actionType switch {
                KnownCollectionRuleActions.CollectDump => collectDump(),
                KnownCollectionRuleActions.CollectExceptions => collectExceptions(),
        };

        // public static object NewActionSettings(string actionType) => 
        //     actionType switch {
        //         KnownCollectionRuleActions.CollectDump => new CollectDumpOptions(),
        //         KnownCollectionRuleActions.CollectExceptions => new CollectExceptionsOptions(),
        //         _ => null
        //     };

        // public static Action SelectAction(
        //     string actionType,
        //     Action<CollectDumpOptions> collectDump,
        //     Action<CollectExceptionsOptions> collectExceptions
        // ) => actionType switch {
        //         KnownCollectionRuleActions.CollectDump => collectDump,
        //         KnownCollectionRuleActions.CollectExceptions => collectExceptions,
        // };


        public static void BindActionSettings(IConfigurationSection actionSection, CollectionRuleActionOptions actionOptions)
        {
            if (actionOptions is null)
                return;

            actionSection.Bind(actionOptions);
            IConfigurationSection settingsSection = actionSection.GetSection(nameof(CollectionRuleActionOptions.Settings));
            switch (actionOptions.Type)
            {
                case KnownCollectionRuleActions.CollectDump: {
                    var actionSettings = new CollectDumpOptions();
                    settingsSection.Bind(actionSettings);
                    actionOptions.Settings = actionSettings;
                    break;
                }
                case KnownCollectionRuleActions.CollectExceptions: {
                    var actionSettings = new CollectExceptionsOptions();
                    settingsSection.Bind(actionSettings);
                    actionOptions.Settings = actionSettings;
                    break;
                }
                case KnownCollectionRuleActions.CollectGCDump: {
                    var actionSettings = new CollectGCDumpOptions();
                    settingsSection.Bind(actionSettings);
                    actionOptions.Settings = actionSettings;
                    break;
                }
                case KnownCollectionRuleActions.CollectLiveMetrics: {
                    var actionSettings = new CollectLiveMetricsOptions();
                    settingsSection.Bind(actionSettings);
                    actionOptions.Settings = actionSettings;
                    break;
                }
                case KnownCollectionRuleActions.CollectLogs: {
                    var actionSettings = new CollectLogsOptions();
                    settingsSection.Bind(actionSettings);
                    actionOptions.Settings = actionSettings;
                    break;
                }
                case KnownCollectionRuleActions.CollectStacks: {
                    var actionSettings = new CollectStacksOptions();
                    settingsSection.Bind(actionSettings);
                    actionOptions.Settings = actionSettings;
                    break;
                }
                case KnownCollectionRuleActions.CollectTrace: {
                    var actionSettings = new CollectTraceOptions();
                    settingsSection.Bind(actionSettings);
                    actionOptions.Settings = actionSettings;
                    break;
                }
                case KnownCollectionRuleActions.Execute: {
                    var actionSettings = new ExecuteOptions();
                    settingsSection.Bind(actionSettings);
                    actionOptions.Settings = actionSettings;
                    break;
                }
                case KnownCollectionRuleActions.LoadProfiler: {
                    var actionSettings = new LoadProfilerOptions();
                    settingsSection.Bind(actionSettings);
                    actionOptions.Settings = actionSettings;
                    break;
                }
                case KnownCollectionRuleActions.SetEnvironmentVariable: {
                    var actionSettings = new SetEnvironmentVariableOptions();
                    settingsSection.Bind(actionSettings);
                    actionOptions.Settings = actionSettings;
                    break;
                }
                case KnownCollectionRuleActions.GetEnvironmentVariable: {
                    var actionSettings = new GetEnvironmentVariableOptions();
                    settingsSection.Bind(actionSettings);
                    actionOptions.Settings = actionSettings;
                    break;
                }
            }
        }

        public static void BindTriggerSettings(IConfigurationSection triggerSection, CollectionRuleTriggerOptions triggerOptions, ICollectionRuleTriggerOperations triggerOperations)
        {
            if (triggerOptions is null)
                return;

            triggerSection.Bind(triggerOptions);
            IConfigurationSection settingsSection = triggerSection.GetSection(nameof(CollectionRuleTriggerOptions.Settings));
            switch (triggerOptions.Type)
            {
                case KnownCollectionRuleTriggers.AspNetRequestCount: {
                    var triggerSettings = new AspNetRequestCountOptions();
                    settingsSection.Bind(triggerSettings);
                    triggerOptions.Settings = triggerSettings;
                    break;
                }
                case KnownCollectionRuleTriggers.AspNetRequestDuration: {
                    var triggerSettings = new AspNetRequestDurationOptions();
                    settingsSection.Bind(triggerSettings);
                    triggerOptions.Settings = triggerSettings;
                    break;
                }
                case KnownCollectionRuleTriggers.AspNetResponseStatus: {
                    var triggerSettings = new AspNetResponseStatusOptions();
                    settingsSection.Bind(triggerSettings);
                    triggerOptions.Settings = triggerSettings;
                    break;
                }
                case KnownCollectionRuleTriggers.EventCounter: {
                    var triggerSettings = new EventCounterOptions();
                    settingsSection.Bind(triggerSettings);
                    triggerOptions.Settings = triggerSettings;
                    break;
                }
                case KnownCollectionRuleTriggers.CPUUsage: {
                    var triggerSettings = new CPUUsageOptions();
                    settingsSection.Bind(triggerSettings);
                    triggerOptions.Settings = triggerSettings;
                    break;
                }
                case KnownCollectionRuleTriggers.GCHeapSize: {
                    var triggerSettings = new GCHeapSizeOptions();
                    settingsSection.Bind(triggerSettings);
                    triggerOptions.Settings = triggerSettings;
                    break;
                }
                case KnownCollectionRuleTriggers.ThreadpoolQueueLength: {
                    var triggerSettings = new ThreadpoolQueueLengthOptions();
                    settingsSection.Bind(triggerSettings);
                    triggerOptions.Settings = triggerSettings;
                    break;
                }
                case KnownCollectionRuleTriggers.EventMeter: {
                    var triggerSettings = new EventMeterOptions();
                    settingsSection.Bind(triggerSettings);
                    triggerOptions.Settings = triggerSettings;
                    break;
                }
            }
        }
    }
}
