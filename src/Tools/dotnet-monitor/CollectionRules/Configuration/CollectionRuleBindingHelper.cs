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
        // public static object CreateActionSettings(
        //     string actionType,
        //     Func<CollectDumpOptions> collectDump,
        //     Func<CollectExceptionsOptions> collectExceptions
        // ) => actionType switch {
        //         KnownCollectionRuleActions.CollectDump => collectDump(),
        //         KnownCollectionRuleActions.CollectExceptions => collectExceptions(),
        //         _ => throw new ArgumentException($"Unknown action type: {actionType}", nameof(actionType))
        // };

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


        public static void BindActionSettings(IConfigurationSection actionSection, CollectionRuleActionOptions actionOptions, ICollectionRuleActionOperations actionOperations)
        {
            if (null != actionOptions &&
                actionOperations.TryBindOptions(actionOptions.Type, actionSection, out object actionSettings))
            {
                actionOptions.Settings = actionSettings;
            }
        }

        public static void BindTriggerSettings(IConfigurationSection triggerSection, CollectionRuleTriggerOptions triggerOptions, ICollectionRuleTriggerOperations triggerOperations)
        {
            if (null != triggerOptions &&
                triggerOperations.TryBindOptions(triggerOptions.Type, triggerSection, out object triggerSettings))
            {
                triggerOptions.Settings = triggerSettings;
            }
        }
    }
}
