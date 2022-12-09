// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Triggers;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Configuration
{
    internal static class CollectionRuleBindingHelper
    {
        public static void BindActionSettings(IConfigurationSection actionSection, CollectionRuleActionOptions actionOptions, ICollectionRuleActionOperations actionOperations)
        {
            if (null != actionOptions &&
                actionOperations.TryCreateOptions(actionOptions.Type, out object actionSettings))
            {
                IConfigurationSection settingsSection = actionSection.GetSection(nameof(CollectionRuleActionOptions.Settings));

                settingsSection.Bind(actionSettings);

                actionOptions.Settings = actionSettings;
            }
        }

        public static void BindTriggerSettings(IConfigurationSection triggerSection, CollectionRuleTriggerOptions triggerOptions, ICollectionRuleTriggerOperations triggerOperations)
        {
            if (null != triggerOptions &&
                triggerOperations.TryCreateOptions(triggerOptions.Type, out object triggerSettings))
            {
                IConfigurationSection settingsSection = triggerSection.GetSection(nameof(CollectionRuleTriggerOptions.Settings));

                settingsSection.Bind(triggerSettings);

                triggerOptions.Settings = triggerSettings;
            }
        }
    }
}
