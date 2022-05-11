// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Triggers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Configuration
{
    internal sealed class CollectionRulePostConfigureOptions :
        IPostConfigureOptions<CollectionRuleOptions>
    {
        private readonly ICollectionRuleActionOperations _actionOperations;
        private readonly ICollectionRuleTriggerOperations _triggerOperations;
        private readonly TemplateOptions _templateOptions;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CollectionRulePostConfigureOptions> _logger;

        public CollectionRulePostConfigureOptions(
            IOptionsMonitor<TemplateOptions> templateOptions,
            IConfiguration configuration,
            ICollectionRuleActionOperations actionOperations,
            ICollectionRuleTriggerOperations triggerOperations,
            ILogger<CollectionRulePostConfigureOptions> logger)
        {
            _templateOptions = templateOptions.CurrentValue;
            _configuration = configuration;
            _actionOperations = actionOperations;
            _triggerOperations = triggerOperations;
            _logger = logger;
        }

        public void PostConfigure(string name, CollectionRuleOptions options)
        {
            bool validActionList = ResolveActionList(options, name);
            bool validTrigger = ResolveTrigger(options, name);
            bool validFilterList = ResolveFilterList(options, name);
            bool validLimits = ResolveLimits(options, name);

            // If any template substitutions fail, clear out the options so that the rule will fail Validation
            if (!(validActionList && validTrigger && validFilterList && validLimits))
            {
                options.Trigger = null;
                options.Actions.Clear();
                options.Filters.Clear();
                options.Limits = null;
            }
        }

        private bool ResolveActionList(CollectionRuleOptions ruleOptions, string name)
        {
            ruleOptions.Actions.Clear();

            IConfigurationSection actionsSection = _configuration.GetSection(ConfigurationPath.Combine(nameof(RootOptions.CollectionRules), name, nameof(CollectionRuleOptions.Actions)));

            foreach (IConfigurationSection actionSection in actionsSection.GetChildren())
            {
                if (!string.IsNullOrEmpty(actionSection.Value))
                {
                    if (!TryGetTemplate(_templateOptions.CollectionRuleActions, actionSection.Value, out CollectionRuleActionOptions templateActionOptions))
                    {
                        return false;
                    }

                    ruleOptions.Actions.Add(templateActionOptions);
                }
                else
                {
                    CollectionRuleActionOptions actionOptions = new();

                    actionSection.Bind(actionOptions);

                    BindActionSettings(actionSection, actionOptions);

                    ruleOptions.Actions.Add(actionOptions);
                }
            }

            return true;
        }

        private bool ResolveTrigger(CollectionRuleOptions ruleOptions, string name)
        {
            IConfigurationSection section = _configuration.GetSection(ConfigurationPath.Combine(nameof(RootOptions.CollectionRules), name, nameof(CollectionRuleOptions.Trigger)));

            if (section.Exists() && !string.IsNullOrEmpty(section.Value))
            {
                if (!TryGetTemplate(_templateOptions.CollectionRuleTriggers, section.Value, out CollectionRuleTriggerOptions triggerOptions))
                {
                    return false;
                }

                ruleOptions.Trigger = triggerOptions;
            }
            else
            {
                BindTriggerSettings(section, ruleOptions.Trigger);
            }

            return true;
        }

        private bool ResolveFilterList(CollectionRuleOptions ruleOptions, string name)
        {
            ruleOptions.Filters.Clear();

            IConfigurationSection filtersSections = _configuration.GetSection(ConfigurationPath.Combine(nameof(RootOptions.CollectionRules), name, nameof(CollectionRuleOptions.Filters)));

            foreach (IConfigurationSection filtersSection in filtersSections.GetChildren())
            {
                if (!string.IsNullOrEmpty(filtersSection.Value))
                {
                    if (!TryGetTemplate(_templateOptions.CollectionRuleFilters, filtersSection.Value, out ProcessFilterDescriptor templateFilterOptions))
                    {
                        return false;
                    }

                    ruleOptions.Filters.Add(templateFilterOptions);
                }
                else
                {
                    ProcessFilterDescriptor filterOptions = new();

                    filtersSection.Bind(filterOptions);

                    ruleOptions.Filters.Add(filterOptions);
                }
            }

            return true;
        }

        private bool ResolveLimits(CollectionRuleOptions ruleOptions, string name)
        {
            IConfigurationSection section = _configuration.GetSection(ConfigurationPath.Combine(nameof(RootOptions.CollectionRules), name, nameof(CollectionRuleOptions.Limits)));
            if (section.Exists() && !string.IsNullOrEmpty(section.Value))
            {
                if (!TryGetTemplate(_templateOptions.CollectionRuleLimits, section.Value, out CollectionRuleLimitsOptions limitsOptions))
                {
                    return false;
                }

                ruleOptions.Limits = limitsOptions;
            }

            return true;
        }

        private bool TryGetTemplate<T>(IDictionary<string, T> templatesOptions, string templateKey, out T templatesValue)
        {
            if (templatesOptions.TryGetValue(templateKey, out templatesValue))
            {
                return true;
            }

            _logger.LogError(Strings.ErrorMessage_TemplateNotFound, templateKey);
            return false;
        }

        private void BindActionSettings(IConfigurationSection actionSection, CollectionRuleActionOptions actionOptions)
        {
            if (null != actionOptions &&
                _actionOperations.TryCreateOptions(actionOptions.Type, out object actionSettings))
            {
                IConfigurationSection settingsSection = actionSection.GetSection(
                    nameof(CollectionRuleActionOptions.Settings));

                settingsSection.Bind(actionSettings);
                actionOptions.Settings = actionSettings;
            }
        }

        private void BindTriggerSettings(IConfigurationSection triggerSection, CollectionRuleTriggerOptions triggerOptions)
        {
            if (null != triggerOptions &&
                _triggerOperations.TryCreateOptions(triggerOptions.Type, out object triggerSettings))
            {
                IConfigurationSection settingsSection = triggerSection.GetSection(nameof(CollectionRuleTriggerOptions.Settings));

                settingsSection.Bind(triggerSettings);

                triggerOptions.Settings = triggerSettings;
            }
        }
    }
}
