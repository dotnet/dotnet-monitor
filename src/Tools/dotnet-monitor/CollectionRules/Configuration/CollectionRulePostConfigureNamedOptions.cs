// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Configuration
{
    internal sealed class CollectionRulePostConfigureOptions :
        IPostConfigureOptions<CollectionRuleOptions>
    {
        private readonly IOptionsMonitor<CustomShortcutOptions> _shortcutOptions;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CollectionRulePostConfigureOptions> _logger;

        public CollectionRulePostConfigureOptions(
            IOptionsMonitor<CustomShortcutOptions> shortcutOptions,
            IConfiguration configuration,
            ILogger<CollectionRulePostConfigureOptions> logger)
        {
            _shortcutOptions = shortcutOptions;
            _configuration = configuration;
            _logger = logger;
        }

        public void PostConfigure(string name, CollectionRuleOptions options)
        {
            if (_shortcutOptions.CurrentValue != null)
            {
                bool actionInsertion = InsertCustomActionsIntoActionList(options, name);
                bool triggerInsertion = InsertCustomTriggerIntoTrigger(options, name);
                bool filterInsertion = InsertCustomFiltersIntoFilterList(options, name);
                bool limitInsertion = InsertCustomLimitIntoLimit(options, name);

                // If any insertions fail, clear out the options so that the rule will fail Validation
                if (!(actionInsertion && triggerInsertion && filterInsertion && limitInsertion))
                {
                    options.Trigger = null;
                    options.Actions.Clear();
                    options.Filters.Clear();
                    options.Limits = null;
                }
            }
        }

        private bool InsertCustomActionsIntoActionList(CollectionRuleOptions ruleOptions, string name)
        {
            var section = _configuration.GetSection(ConfigurationPath.Combine(nameof(RootOptions.CollectionRules), name, nameof(CollectionRuleOptions.Actions)));

            var actionSections = section.GetChildren();

            for (int index = 0; index < actionSections.Count(); ++index)
            {
                if (!string.IsNullOrEmpty(actionSections.ElementAt(index).Value))
                {
                    if (!InsertCustomShortcut(_shortcutOptions.CurrentValue.Actions, actionSections.ElementAt(index).Value, ruleOptions.Actions, index))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool InsertCustomTriggerIntoTrigger(CollectionRuleOptions ruleOptions, string name)
        {
            IConfigurationSection section = _configuration.GetSection(ConfigurationPath.Combine(nameof(RootOptions.CollectionRules), name, nameof(CollectionRuleOptions.Trigger)));
            if (section.Exists() && !string.IsNullOrEmpty(section.Value))
            {
                if (!ValidateCustomShortcutExists(_shortcutOptions.CurrentValue.Triggers, section.Value))
                {
                    return false;
                }

                CollectionRuleTriggerOptions options = _shortcutOptions.CurrentValue.Triggers[section.Value];
                ruleOptions.Trigger = options;
            }

            return true;
        }

        private bool InsertCustomFiltersIntoFilterList(CollectionRuleOptions ruleOptions, string name)
        {
            var section = _configuration.GetSection(ConfigurationPath.Combine(nameof(RootOptions.CollectionRules), name, nameof(CollectionRuleOptions.Filters)));

            var filterSections = section.GetChildren();

            for (int index = 0; index < filterSections.Count(); ++index)
            {
                if (!string.IsNullOrEmpty(filterSections.ElementAt(index).Value))
                {
                    if (!InsertCustomShortcut(_shortcutOptions.CurrentValue.Filters, filterSections.ElementAt(index).Value, ruleOptions.Filters, index))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool InsertCustomLimitIntoLimit(CollectionRuleOptions ruleOptions, string name)
        {
            IConfigurationSection section = _configuration.GetSection(ConfigurationPath.Combine(nameof(RootOptions.CollectionRules), name, nameof(CollectionRuleOptions.Limits)));
            if (section.Exists() && !string.IsNullOrEmpty(section.Value))
            {
                if (!ValidateCustomShortcutExists(_shortcutOptions.CurrentValue.Limits, section.Value))
                {
                    return false;
                }

                CollectionRuleLimitsOptions options = _shortcutOptions.CurrentValue.Limits[section.Value];
                ruleOptions.Limits = options;
            }

            return true;
        }

        private bool InsertCustomShortcut<T>(IDictionary<string, T> shortcutsOptions, string shortcutKey, List<T> options, int index) where T : class
        {
            if (!ValidateCustomShortcutExists(shortcutsOptions, shortcutKey))
            {
                return false;
            }

            options.Insert(index, shortcutsOptions[shortcutKey]);
            return true;
        }

        private bool ValidateCustomShortcutExists<T>(IDictionary<string, T> shortcutsOptions, string shortcutKey)
        {
            if (shortcutsOptions.ContainsKey(shortcutKey))
            {
                return true;
            }

            _logger.LogError(Strings.ErrorMessage_CustomShortcutNotFound, shortcutKey);
            return false;
        }
    }
}
