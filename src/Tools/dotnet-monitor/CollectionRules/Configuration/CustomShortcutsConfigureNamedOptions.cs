// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Triggers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Configuration
{
    internal sealed class CustomShortcutsConfigureOptions :
        IConfigureOptions<CustomShortcutOptions>
    {
        private readonly ICollectionRuleTriggerOperations _triggerOperations;
        private readonly ICollectionRuleActionOperations _actionOperations;
        private readonly IConfiguration _configuration;

        public CustomShortcutsConfigureOptions(
            ICollectionRuleTriggerOperations triggerOperations,
            ICollectionRuleActionOperations actionOperations,
            IConfiguration configuration)
        {
            _triggerOperations = triggerOperations;
            _actionOperations = actionOperations;
            _configuration = configuration;
        }

        public void Configure(CustomShortcutOptions options)
        {
            // Action Shortcuts
            foreach (var key in options.Actions.Keys)
            {
                IConfigurationSection section = _configuration.GetSection(ConfigurationPath.Combine(nameof(RootOptions.CustomShortcuts), nameof(CustomShortcutOptions.Actions), key));

                if (section.Exists())
                {
                    section.Bind(options);

                    BindCustomActions(section, options, key);
                }
            }

            // Trigger Shortcuts
            foreach (var key in options.Triggers.Keys)
            {
                IConfigurationSection section = _configuration.GetSection(ConfigurationPath.Combine(nameof(RootOptions.CustomShortcuts), nameof(CustomShortcutOptions.Triggers), key));

                if (section.Exists())
                {
                    section.Bind(options);

                    BindCustomTriggers(section, options, key);
                }
            }
        }

        private void BindCustomActions(IConfigurationSection ruleSection, CustomShortcutOptions shortcutOptions, string shortcutName)
        {
            CollectionRuleActionOptions actionOptions = shortcutOptions.Actions[shortcutName];

            if (null != actionOptions &&
                _actionOperations.TryCreateOptions(actionOptions.Type, out object actionSettings))
            {
                IConfigurationSection settingsSection = ruleSection.GetSection(ConfigurationPath.Combine(
                    nameof(CollectionRuleActionOptions.Settings)));

                settingsSection.Bind(actionSettings);

                actionOptions.Settings = actionSettings;
            }
        }

        private void BindCustomTriggers(IConfigurationSection ruleSection, CustomShortcutOptions shortcutOptions, string shortcutName)
        {
            CollectionRuleTriggerOptions triggerOptions = shortcutOptions.Triggers[shortcutName];

            if (null != triggerOptions &&
                _triggerOperations.TryCreateOptions(triggerOptions.Type, out object triggerSettings))
            {
                IConfigurationSection settingsSection = ruleSection.GetSection(ConfigurationPath.Combine(
                    nameof(CollectionRuleTriggerOptions.Settings)));

                settingsSection.Bind(triggerSettings);

                triggerOptions.Settings = triggerSettings;
            }
        }
    }
}
