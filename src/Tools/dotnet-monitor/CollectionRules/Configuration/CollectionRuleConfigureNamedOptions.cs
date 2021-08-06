// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Configuration
{
    internal sealed class CollectionRuleConfigureNamedOptions :
        IConfigureNamedOptions<CollectionRuleOptions>
    {
        private readonly ICollectionRuleActionOptionsProvider _actionOptionsProvider;
        private readonly IConfigurationSection _collectionRulesSection;
        private readonly ICollectionRuleTriggerOptionsProvider _triggerOptionsProvider;

        public CollectionRuleConfigureNamedOptions(
            IConfiguration configuration,
            ICollectionRuleActionOptionsProvider actionOptionsProvider,
            ICollectionRuleTriggerOptionsProvider triggerOptionsProvider)
        {
            _actionOptionsProvider = actionOptionsProvider;
            _collectionRulesSection = configuration.GetSection(nameof(ConfigurationKeys.CollectionRules));
            _triggerOptionsProvider = triggerOptionsProvider;
        }

        public void Configure(string name, CollectionRuleOptions options)
        {
            IConfigurationSection ruleSection = _collectionRulesSection.GetSection(name);
            Debug.Assert(ruleSection.Exists());
            if (ruleSection.Exists())
            {
                ruleSection.Bind(options);

                BindTriggerSettings(ruleSection, options);

                for (int i = 0; i < options.Actions.Count; i++)
                {
                    BindActionSettings(ruleSection, options, i);
                }
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
                _actionOptionsProvider.TryGetOptionsType(actionOptions.Type, out Type optionsType) &&
                null != optionsType)
            {
                object actionSettings = Activator.CreateInstance(optionsType);

                IConfigurationSection settingsSection = ruleSection.GetSection(ConfigurationPath.Combine(
                    nameof(CollectionRuleOptions.Actions),
                    actionIndex.ToString(CultureInfo.InvariantCulture),
                    nameof(CollectionRuleActionOptions.Settings)));

                settingsSection.Bind(actionSettings);

                actionOptions.Settings = actionSettings;
            }
        }

        private void BindTriggerSettings(IConfigurationSection ruleSection, CollectionRuleOptions ruleOptions)
        {
            CollectionRuleTriggerOptions triggerOptions = ruleOptions.Trigger;

            if (null != triggerOptions &&
                _triggerOptionsProvider.TryGetOptionsType(triggerOptions.Type, out Type optionsType) &&
                null != optionsType)
            {
                object triggerSettings = Activator.CreateInstance(optionsType);

                IConfigurationSection settingsSection = ruleSection.GetSection(ConfigurationPath.Combine(
                    nameof(CollectionRuleOptions.Trigger),
                    nameof(CollectionRuleTriggerOptions.Settings)));

                settingsSection.Bind(triggerSettings);

                triggerOptions.Settings = triggerSettings;
            }
        }
    }
}
