// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers.EventCounterShortcuts;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Triggers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
        private readonly IServiceProvider _serviceProvider;

        public CollectionRuleConfigureNamedOptions(
            CollectionRulesConfigurationProvider configurationProvider,
            ICollectionRuleActionOperations actionOperations,
            ICollectionRuleTriggerOperations triggerOperations,
            IServiceProvider serviceProvider,
            ILogger<CollectionRuleConfigureNamedOptions> logger)
        {
            _actionOperations = actionOperations;
            _configurationProvider = configurationProvider;
            _triggerOperations = triggerOperations;
            _serviceProvider = serviceProvider;
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
                _triggerOperations.TryCreateOptions(triggerOptions.Type, out object triggerSettings))
            {
                IConfigurationSection settingsSection = ruleSection.GetSection(ConfigurationPath.Combine(
                    nameof(CollectionRuleOptions.Trigger),
                    nameof(CollectionRuleTriggerOptions.Settings)));

                settingsSection.Bind(triggerSettings);

                triggerOptions.Settings = triggerSettings;

                if (triggerSettings is IEventCounterShortcuts shortcutsOptions)
                {
                    EventCounterOptions eventCounterOptions = TranslateShortcutToEventCounter(shortcutsOptions);
                    
                    if (null != eventCounterOptions)
                    {
                        triggerOptions.Type = KnownCollectionRuleTriggers.EventCounter;
                        triggerOptions.Settings = eventCounterOptions;
                    }
                }
            }
        }

        private EventCounterOptions TranslateShortcutToEventCounter(IEventCounterShortcuts options)
        {
            EventCounterOptions eventCounterOptions = new EventCounterOptions();

            eventCounterOptions.ProviderName = IEventCounterShortcutsConstants.SystemRuntime;

            double? greaterThanDefault = null;
            double? lessThanDefault = null;
            TimeSpan? slidingWindowDurationDefault = null;

            ValidateOptionsResult validationResult = new();

            if (options is CPUUsageOptions)
            {
                validationResult = ValidateShortcut<CPUUsageOptions>(options);

                eventCounterOptions.CounterName = IEventCounterShortcutsConstants.CPUUsage;
                greaterThanDefault = CPUUsageOptionsDefaults.GreaterThan;
            }
            else if (options is GCHeapSizeOptions)
            {
                validationResult = ValidateShortcut<GCHeapSizeOptions>(options);

                eventCounterOptions.CounterName = IEventCounterShortcutsConstants.GCHeapSize;
                greaterThanDefault = GCHeapSizeOptionsDefaults.GreaterThan;
            }
            else if (options is ThreadpoolQueueLengthOptions)
            {
                validationResult = ValidateShortcut<ThreadpoolQueueLengthOptions>(options);

                eventCounterOptions.CounterName = IEventCounterShortcutsConstants.ThreadpoolQueueLength;
                greaterThanDefault = ThreadpoolQueueLengthOptionsDefaults.GreaterThan;
            }

            if (validationResult.Failed)
            {
                return null; // Don't do the transformation -> allows the proper Validation message to be logged.
            }

            eventCounterOptions.GreaterThan = options.LessThan.HasValue ? options.GreaterThan : (options.GreaterThan ?? greaterThanDefault);
            eventCounterOptions.LessThan = options.GreaterThan.HasValue ? options.LessThan : (options.LessThan ?? lessThanDefault);
            eventCounterOptions.SlidingWindowDuration = options.SlidingWindowDuration ?? slidingWindowDurationDefault;

            return eventCounterOptions;
        }

        private ValidateOptionsResult ValidateShortcut<T>(IEventCounterShortcuts options) where T : class
        {
            DataAnnotationValidateOptions<T> validateOptions = new(_serviceProvider);
            return validateOptions.Validate(string.Empty, (T)options);
        }
    }
}
