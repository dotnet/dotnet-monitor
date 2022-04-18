// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Triggers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Globalization;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Configuration
{
    internal sealed class CollectionRuleConfigureNamedOptions :
        IConfigureNamedOptions<CollectionRuleOptions>
    {
        private readonly ICollectionRuleActionOperations _actionOperations;
        private readonly CollectionRulesConfigurationProvider _configurationProvider;
        private readonly ICollectionRuleTriggerOperations _triggerOperations;
        private readonly IConfiguration _configuration;

        public CollectionRuleConfigureNamedOptions(
            CollectionRulesConfigurationProvider configurationProvider,
            ICollectionRuleActionOperations actionOperations,
            ICollectionRuleTriggerOperations triggerOperations,
            IConfiguration configuration)
        {
            _actionOperations = actionOperations;
            _configurationProvider = configurationProvider;
            _triggerOperations = triggerOperations;
            _configuration = configuration;
        }

        public void Configure(string name, CollectionRuleOptions options)
        {
            IConfigurationSection ruleSection = _configurationProvider.GetCollectionRuleSection(name);
            if (ruleSection.Exists())
            {
                ruleSection.Bind(options);

                BindTriggerSettings(ruleSection, options);

                for (int i = 0; i < options.Actions.Count; i++)
                {
                    BindActionSettings(ruleSection, options, i);
                }

                BindCustomActions(ruleSection, options, name);
            }
        }

        public void Configure(CollectionRuleOptions options)
        {
            throw new NotSupportedException();
        }

        private void BindActionSettings(IConfigurationSection ruleSection, CollectionRuleOptions ruleOptions, int actionIndex)
        {
            CollectionRuleActionOptions actionOptions = ruleOptions.Actions[actionIndex];

            if (null != actionOptions &&
                _actionOperations.TryCreateOptions(actionOptions.Type, out object actionSettings))
            {
                IConfigurationSection settingsSection = ruleSection.GetSection(ConfigurationPath.Combine(
                    nameof(CollectionRuleOptions.Actions),
                    actionIndex.ToString(CultureInfo.InvariantCulture),
                    nameof(CollectionRuleActionOptions.Settings)));

                settingsSection.Bind(actionSettings);

                actionOptions.Settings = actionSettings;
            }
        }

        private void BindCustomActions(IConfigurationSection ruleSection, CollectionRuleOptions ruleOptions, string name)
        {
            // Won't do it this way - will determine how many actions there are and then keep pulling until we hit an action with no value
            for (int index = 0; index < 100; ++index)
            {
                IConfigurationSection section = _configuration.GetSection($"CollectionRules:{name}:Actions:{index}");
                if (section.Exists() && !string.IsNullOrEmpty(section.Value))
                {
                    // Translate the value into the corresponding custom shortcut -> should we do this through the config, or more directly from CustomShortcutsOptions -> CollectionRuleOptions
                    IConfigurationSection customSection = _configuration.GetSection($"CustomShortcuts:Actions:{section.Value}");

                    IConfigurationSection typeSection = customSection.GetSection("Type");
                    IConfigurationSection settingsSection = customSection.GetSection("Settings");
                    IConfigurationSection nameSection = customSection.GetSection("Name");
                    IConfigurationSection wfcSection = customSection.GetSection("WaitForCompletion");

                    CollectionRuleActionOptions newActionOptions = new();

                    if (null != newActionOptions &&
                        typeSection.Exists() &&
                        !string.IsNullOrEmpty(typeSection.Value) &&
                        _actionOperations.TryCreateOptions(typeSection.Value, out object newActionSettings))
                    {
                        newActionOptions.Type = typeSection.Value;
                        newActionOptions.Name = nameSection.Value;
                        bool.TryParse(wfcSection.Value, out bool wfcValue); // type conversion could be an issue
                        newActionOptions.WaitForCompletion = wfcValue;

                        settingsSection.Bind(newActionSettings);

                        newActionOptions.Settings = newActionSettings;

                        ruleOptions.Actions.Add(newActionOptions);
                    }
                }
            }
        }

        private void BindTriggerSettings(IConfigurationSection ruleSection, CollectionRuleOptions ruleOptions)
        {
            CollectionRuleTriggerOptions triggerOptions = ruleOptions.Trigger;

            if (null != triggerOptions &&
                _triggerOperations.TryCreateOptions(triggerOptions.Type, out object triggerSettings))
            {
                IConfigurationSection settingsSection = ruleSection.GetSection(ConfigurationPath.Combine(
                    nameof(CollectionRuleOptions.Trigger),
                    nameof(CollectionRuleTriggerOptions.Settings)));

                settingsSection.Bind(triggerSettings);

                triggerOptions.Settings = triggerSettings;
            }
        }
    }
}
