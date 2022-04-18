// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Triggers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Configuration
{
    internal sealed class CustomShortcutsConfigureOptions :
        IPostConfigureOptions<CollectionRuleOptions>
    {
        private readonly ICollectionRuleTriggerOperations _triggerOperations;
        private readonly ICollectionRuleActionOperations _actionOperations;
        private readonly IOptionsMonitor<CustomShortcutOptions> _shortcutOptions;
        private readonly IConfiguration _configuration;

        public CustomShortcutsConfigureOptions(
            ICollectionRuleTriggerOperations triggerOperations,
            ICollectionRuleActionOperations actionOperations,
            IOptionsMonitor<CustomShortcutOptions> shortcutOptions,
            IConfiguration configuration
            )
        {
            _triggerOperations = triggerOperations;
            _actionOperations = actionOperations;
            _shortcutOptions = shortcutOptions;
            _configuration = configuration;
        }

        public void PostConfigure(string name, CollectionRuleOptions options)
        {
            // Action Shortcuts
            foreach (var key in _shortcutOptions.CurrentValue.Actions.Keys)
            {
                IConfigurationSection section = _configuration.GetSection($"{nameof(RootOptions.CustomShortcuts)}:{nameof(CustomShortcutOptions.Actions)}:{key}");

                if (section.Exists())
                {
                    section.Bind(_shortcutOptions);

                    BindCustomActions(section, _shortcutOptions.CurrentValue, key);
                }
            }

            // Trigger Shortcuts
            foreach (var key in _shortcutOptions.CurrentValue.Triggers.Keys)
            {
                IConfigurationSection section = _configuration.GetSection($"{nameof(RootOptions.CustomShortcuts)}:{nameof(CustomShortcutOptions.Triggers)}:{key}");

                if (section.Exists())
                {
                    section.Bind(_shortcutOptions);

                    BindCustomTriggers(section, _shortcutOptions.CurrentValue, key);
                }
            }

            InsertCustomActionsIntoActionList(options, name);
            InsertCustomTriggerIntoTrigger(options, name);
            InsertCustomFiltersIntoFilterList(options, name);
            InsertCustomLimitIntoLimit(options, name);
        }

        private void InsertCustomActionsIntoActionList(CollectionRuleOptions ruleOptions, string name)
        {
            bool boundAllActions = false;
            int index = 0;

            while (!boundAllActions)
            {
                IConfigurationSection section = _configuration.GetSection($"{nameof(RootOptions.CollectionRules)}:{name}:{nameof(CollectionRuleOptions.Actions)}:{index}");
                if (section.Exists() && !string.IsNullOrEmpty(section.Value))
                {
                    CollectionRuleActionOptions options = _shortcutOptions.CurrentValue.Actions[section.Value]; // Need to make sure this is safe for invalid names
                    ruleOptions.Actions.Add(options);
                }
                else
                {
                    boundAllActions = true;
                }

                ++index;
            }
        }

        private void InsertCustomTriggerIntoTrigger(CollectionRuleOptions ruleOptions, string name)
        {
            IConfigurationSection section = _configuration.GetSection($"{nameof(RootOptions.CollectionRules)}:{name}:{nameof(CollectionRuleOptions.Trigger)}");
            if (section.Exists() && !string.IsNullOrEmpty(section.Value))
            {
                CollectionRuleTriggerOptions options = _shortcutOptions.CurrentValue.Triggers[section.Value]; // Need to make sure this is safe for invalid names
                ruleOptions.Trigger = options;
            }
        }

        private void InsertCustomFiltersIntoFilterList(CollectionRuleOptions ruleOptions, string name)
        {
            bool boundAllFilters = false;
            int index = 0;

            while (!boundAllFilters)
            {
                IConfigurationSection section = _configuration.GetSection($"{nameof(RootOptions.CollectionRules)}:{name}:{nameof(CollectionRuleOptions.Filters)}:{index}");
                if (section.Exists() && !string.IsNullOrEmpty(section.Value))
                {
                    ProcessFilterDescriptor options = _shortcutOptions.CurrentValue.Filters[section.Value]; // Need to make sure this is safe for invalid names
                    ruleOptions.Filters.Add(options);
                }
                else
                {
                    boundAllFilters = true;
                }

                ++index;
            }
        }

        private void InsertCustomLimitIntoLimit(CollectionRuleOptions ruleOptions, string name)
        {
            IConfigurationSection section = _configuration.GetSection($"{nameof(RootOptions.CollectionRules)}:{name}:{nameof(CollectionRuleOptions.Limits)}");
            if (section.Exists() && !string.IsNullOrEmpty(section.Value))
            {
                CollectionRuleLimitsOptions options = _shortcutOptions.CurrentValue.Limits[section.Value]; // Need to make sure this is safe for invalid names
                ruleOptions.Limits = options;
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