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
    internal sealed class TemplatesPostConfigureOptions :
        IPostConfigureOptions<TemplateOptions>
    {
        private readonly ICollectionRuleTriggerOperations _triggerOperations;
        private readonly ICollectionRuleActionOperations _actionOperations;
        private readonly IConfiguration _configuration;

        public TemplatesPostConfigureOptions(
            ICollectionRuleTriggerOperations triggerOperations,
            ICollectionRuleActionOperations actionOperations,
            IConfiguration configuration)
        {
            _triggerOperations = triggerOperations;
            _actionOperations = actionOperations;
            _configuration = configuration;
        }

        public void PostConfigure(string name, TemplateOptions options)
        {
            // Action Shortcuts
            foreach (var key in options.CollectionRuleActions.Keys)
            {
                IConfigurationSection section = _configuration.GetSection(ConfigurationPath.Combine(nameof(RootOptions.Templates), nameof(TemplateOptions.CollectionRuleActions), key));

                if (section.Exists())
                {
                    BindCustomActions(section, options, key);
                }
            }

            // Trigger Shortcuts
            foreach (var key in options.CollectionRuleTriggers.Keys)
            {
                IConfigurationSection section = _configuration.GetSection(ConfigurationPath.Combine(nameof(RootOptions.Templates), nameof(TemplateOptions.CollectionRuleTriggers), key));

                if (section.Exists())
                {
                    BindCustomTriggers(section, options, key);
                }
            }
        }

        private void BindCustomActions(IConfigurationSection ruleSection, TemplateOptions shortcutOptions, string shortcutName)
        {
            CollectionRuleActionOptions actionOptions = shortcutOptions.CollectionRuleActions[shortcutName];

            if (null != actionOptions &&
                _actionOperations.TryCreateOptions(actionOptions.Type, out object actionSettings))
            {
                IConfigurationSection settingsSection = ruleSection.GetSection(nameof(CollectionRuleActionOptions.Settings));

                settingsSection.Bind(actionSettings);

                actionOptions.Settings = actionSettings;
            }
        }

        private void BindCustomTriggers(IConfigurationSection ruleSection, TemplateOptions shortcutOptions, string shortcutName)
        {
            CollectionRuleTriggerOptions triggerOptions = shortcutOptions.CollectionRuleTriggers[shortcutName];

            if (null != triggerOptions &&
                _triggerOperations.TryCreateOptions(triggerOptions.Type, out object triggerSettings))
            {
                IConfigurationSection settingsSection = ruleSection.GetSection(nameof(CollectionRuleTriggerOptions.Settings));

                settingsSection.Bind(triggerSettings);

                triggerOptions.Settings = triggerSettings;
            }
        }
    }
}
