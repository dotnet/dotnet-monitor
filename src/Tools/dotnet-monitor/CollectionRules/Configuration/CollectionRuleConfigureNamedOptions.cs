// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Triggers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Configuration
{
    internal sealed class CollectionRuleConfigureNamedOptions :
        IConfigureNamedOptions<CollectionRuleOptions>
    {
        private readonly ICollectionRuleActionOperations _actionOperations;
        private readonly CollectionRulesConfigurationProvider _configurationProvider;
        private readonly ICollectionRuleTriggerOperations _triggerOperations;

        public CollectionRuleConfigureNamedOptions(
            CollectionRulesConfigurationProvider configurationProvider,
            ICollectionRuleActionOperations actionOperations,
            ICollectionRuleTriggerOperations triggerOperations)
        {
            _actionOperations = actionOperations;
            _configurationProvider = configurationProvider;
            _triggerOperations = triggerOperations;
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
                int index = 0;

                while (0 <= actionIndex)
                {
                    IConfigurationSection actionSection = ruleSection.GetSection(ConfigurationPath.Combine(
                        nameof(CollectionRuleOptions.Actions),
                        index.ToString(CultureInfo.InvariantCulture)));

                    if (actionSection.Exists() && string.IsNullOrEmpty(actionSection.Value))
                    {
                        actionIndex -= 1;

                        if (0 > actionIndex)
                        {
                            IConfigurationSection settingsSection = actionSection.GetSection(
                                nameof(CollectionRuleActionOptions.Settings));

                            settingsSection.Bind(actionSettings);
                            actionOptions.Settings = actionSettings;
                        }
                    }

                    ++index;
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
