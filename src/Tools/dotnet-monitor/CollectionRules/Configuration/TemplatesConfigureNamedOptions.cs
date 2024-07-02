// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        private static readonly string collectionRuleActionsPath = ConfigurationPath.Combine(nameof(RootOptions.Templates), nameof(TemplateOptions.CollectionRuleActions));
        private static readonly string collectionRuleTriggersPath = ConfigurationPath.Combine(nameof(RootOptions.Templates), nameof(TemplateOptions.CollectionRuleTriggers));

        public TemplatesPostConfigureOptions(
            ICollectionRuleTriggerOperations triggerOperations,
            ICollectionRuleActionOperations actionOperations,
            IConfiguration configuration)
        {
            _triggerOperations = triggerOperations;
            _actionOperations = actionOperations;
            _configuration = configuration;
        }

        public void PostConfigure(string? name, TemplateOptions options)
        {
            IConfigurationSection collectionRuleActionsSection = _configuration.GetSection(collectionRuleActionsPath);

            if (options.CollectionRuleActions != null)
            {
                foreach (string key in options.CollectionRuleActions.Keys)
                {
                    IConfigurationSection actionSection = collectionRuleActionsSection.GetSection(key);

                    if (actionSection.Exists())
                    {
                        CollectionRuleBindingHelper.BindActionSettings(actionSection, options.CollectionRuleActions[key], _actionOperations);
                    }
                }
            }

            IConfigurationSection collectionRuleTriggersSection = _configuration.GetSection(collectionRuleTriggersPath);

            if (options.CollectionRuleTriggers != null)
            {
                foreach (string key in options.CollectionRuleTriggers.Keys)
                {
                    IConfigurationSection triggerSection = collectionRuleTriggersSection.GetSection(key);

                    if (triggerSection.Exists())
                    {
                        CollectionRuleBindingHelper.BindTriggerSettings(triggerSection, options.CollectionRuleTriggers[key], _triggerOperations);
                    }
                }
            }
        }
    }
}
