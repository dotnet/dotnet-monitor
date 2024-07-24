// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Triggers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Configuration
{
    internal sealed class CollectionRulePostConfigureOptions :
        IPostConfigureOptions<CollectionRuleOptions>
    {
        private readonly ICollectionRuleActionOperations _actionOperations;
        private readonly ICollectionRuleTriggerOperations _triggerOperations;
        private readonly TemplateOptions _templateOptions;
        private readonly IConfiguration _configuration;

        public CollectionRulePostConfigureOptions(
            IOptionsMonitor<TemplateOptions> templateOptions,
            IConfiguration configuration,
            ICollectionRuleActionOperations actionOperations,
            ICollectionRuleTriggerOperations triggerOperations)
        {
            _templateOptions = templateOptions.CurrentValue;
            _configuration = configuration;
            _actionOperations = actionOperations;
            _triggerOperations = triggerOperations;
        }

        public void PostConfigure(string name, CollectionRuleOptions options)
        {
            IConfigurationSection ruleSection = _configuration.GetSection(ConfigurationPath.Combine(nameof(RootOptions.CollectionRules), name));

            ResolveActionList(options, ruleSection);
            ResolveTrigger(options, ruleSection);
            ResolveFilterList(options, ruleSection);
            ResolveLimits(options, ruleSection);
        }

        private void ResolveActionList(CollectionRuleOptions ruleOptions, IConfigurationSection ruleSection)
        {
            ruleOptions.Actions.Clear();

            IConfigurationSection actionSections = ruleSection.GetSection(nameof(CollectionRuleOptions.Actions));

            foreach (IConfigurationSection actionSection in actionSections.GetChildren())
            {
                // The Section Key is the action index; the value (if present) is the name of the template
                if (SectionHasValue(actionSection))
                {
                    TryGetTemplate(ruleOptions, _templateOptions.CollectionRuleActions, actionSection.Value, out CollectionRuleActionOptions templateActionOptions);

                    ruleOptions.Actions.Add(templateActionOptions);
                }
                else
                {
                    CollectionRuleActionOptions actionOptions = new();

                    actionSection.Bind(actionOptions);

                    CollectionRuleBindingHelper.BindActionSettings(actionSection, actionOptions, _actionOperations);

                    ruleOptions.Actions.Add(actionOptions);
                }
            }
        }

        private void ResolveTrigger(CollectionRuleOptions ruleOptions, IConfigurationSection ruleSection)
        {
            IConfigurationSection section = ruleSection.GetSection(nameof(CollectionRuleOptions.Trigger));

            if (SectionHasValue(section))
            {
                TryGetTemplate(ruleOptions, _templateOptions.CollectionRuleTriggers, section.Value, out CollectionRuleTriggerOptions triggerOptions);

                ruleOptions.Trigger = triggerOptions;
            }
            else
            {
                CollectionRuleBindingHelper.BindTriggerSettings(section, ruleOptions.Trigger, _triggerOperations);
            }
        }

        private void ResolveFilterList(CollectionRuleOptions ruleOptions, IConfigurationSection ruleSection)
        {
            ruleOptions.Filters.Clear();

            IConfigurationSection filterSections = ruleSection.GetSection(nameof(CollectionRuleOptions.Filters));

            foreach (IConfigurationSection filterSection in filterSections.GetChildren())
            {
                // The Section Key is the filter index; the value (if present) is the name of the template
                if (SectionHasValue(filterSection))
                {
                    TryGetTemplate(ruleOptions, _templateOptions.CollectionRuleFilters, filterSection.Value, out ProcessFilterDescriptor templateFilterOptions);

                    ruleOptions.Filters.Add(templateFilterOptions);
                }
                else
                {
                    ProcessFilterDescriptor filterOptions = new();

                    filterSection.Bind(filterOptions);

                    ruleOptions.Filters.Add(filterOptions);
                }
            }
        }

        private void ResolveLimits(CollectionRuleOptions ruleOptions, IConfigurationSection ruleSection)
        {
            IConfigurationSection section = ruleSection.GetSection(nameof(CollectionRuleOptions.Limits));

            if (SectionHasValue(section))
            {
                TryGetTemplate(ruleOptions, _templateOptions.CollectionRuleLimits, section.Value, out CollectionRuleLimitsOptions limitsOptions);

                ruleOptions.Limits = limitsOptions;
            }
        }

        private static bool TryGetTemplate<T>(CollectionRuleOptions ruleOptions, IDictionary<string, T> templatesOptions, string templateKey, out T templatesValue) where T : new()
        {
            if (!templatesOptions.TryGetValue(templateKey, out templatesValue))
            {
                templatesValue = new();
                ruleOptions.ErrorList.Add(new ValidationResult(string.Format(CultureInfo.CurrentCulture, Strings.ErrorMessage_TemplateNotFound, templateKey)));
                return false;
            }

            return true;
        }

        private static bool SectionHasValue(IConfigurationSection section)
        {
            // If the section has a value, the value is the name of a template.
            return !string.IsNullOrEmpty(section.Value);
        }
    }
}
