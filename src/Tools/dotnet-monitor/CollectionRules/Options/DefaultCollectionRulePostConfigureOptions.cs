// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.CollectionRuleDefaultsInterfaces;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers;
using Microsoft.Extensions.Options;
using System;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options
{
    internal sealed class DefaultCollectionRulePostConfigureOptions :
        IPostConfigureOptions<CollectionRuleOptions>
    {
        private readonly IOptionsMonitor<CollectionRuleDefaultsOptions> _defaultOptions;
        private static readonly TimeSpan SlidingWindowDurationDefault = TimeSpan.Parse(TriggerOptionsConstants.SlidingWindowDuration_Default);

        public DefaultCollectionRulePostConfigureOptions(
            IOptionsMonitor<CollectionRuleDefaultsOptions> defaultOptions
            )
        {
            _defaultOptions = defaultOptions;
        }

        public void PostConfigure(string? name, CollectionRuleOptions options)
        {
            ConfigureEgress(options);
            ConfigureLimits(options);
            ConfigureRequestCounts(options);
            ConfigureResponseCounts(options);
            ConfigureSlidingWindowDurations(options);
        }

        private void ConfigureEgress(CollectionRuleOptions options)
        {
            CollectionRuleActionDefaultsOptions? actionDefaults = _defaultOptions.CurrentValue.Actions;

            if (actionDefaults == null)
            {
                return;
            }

            foreach (var action in options.Actions)
            {
                if (action.Settings is IEgressProviderProperties egressProviderProperties)
                {
                    if (string.IsNullOrEmpty(egressProviderProperties.Egress))
                    {
#nullable disable
                        egressProviderProperties.Egress = actionDefaults.Egress;
#nullable restore
                    }
                }
            }
        }

        private void ConfigureLimits(CollectionRuleOptions options)
        {
            CollectionRuleLimitsDefaultsOptions? limitsDefaults = _defaultOptions.CurrentValue.Limits;

            if (limitsDefaults == null)
            {
                return;
            }

            if (null == options.Limits)
            {
                if (!limitsDefaults.ActionCount.HasValue
                    && !limitsDefaults.ActionCountSlidingWindowDuration.HasValue
                    && !limitsDefaults.RuleDuration.HasValue)
                {
                    return;
                }

                options.Limits = new CollectionRuleLimitsOptions();
            }

            if (null == options.Limits.ActionCount)
            {
                options.Limits.ActionCount = limitsDefaults.ActionCount ?? CollectionRuleLimitsOptionsDefaults.ActionCount;
            }

            if (null == options.Limits.ActionCountSlidingWindowDuration)
            {
                options.Limits.ActionCountSlidingWindowDuration = limitsDefaults.ActionCountSlidingWindowDuration;
            }

            if (null == options.Limits.RuleDuration)
            {
                options.Limits.RuleDuration = limitsDefaults.RuleDuration;
            }
        }

        private void ConfigureRequestCounts(CollectionRuleOptions options)
        {
            CollectionRuleTriggerDefaultsOptions? triggerDefaults = _defaultOptions.CurrentValue.Triggers;

            if (triggerDefaults == null)
            {
                return;
            }

            if (null != options.Trigger)
            {
                if (options.Trigger.Settings is IRequestCountProperties requestCountProperties)
                {
                    if (0 == requestCountProperties.RequestCount && triggerDefaults.RequestCount.HasValue)
                    {
                        requestCountProperties.RequestCount = triggerDefaults.RequestCount.Value;
                    }
                }
            }
        }

        private void ConfigureResponseCounts(CollectionRuleOptions options)
        {
            CollectionRuleTriggerDefaultsOptions? triggerDefaults = _defaultOptions.CurrentValue.Triggers;

            if (triggerDefaults == null)
            {
                return;
            }

            if (null != options.Trigger)
            {
                if (options.Trigger.Settings is AspNetResponseStatusOptions responseStatusProperties)
                {
                    if (0 == responseStatusProperties.ResponseCount && triggerDefaults.ResponseCount.HasValue)
                    {
                        responseStatusProperties.ResponseCount = triggerDefaults.ResponseCount.Value;
                    }
                }
            }
        }

        private void ConfigureSlidingWindowDurations(CollectionRuleOptions options)
        {
            CollectionRuleTriggerDefaultsOptions? triggerDefaults = _defaultOptions.CurrentValue.Triggers;

            if (triggerDefaults == null)
            {
                return;
            }

            if (null != options.Trigger)
            {
                if (options.Trigger.Settings is ISlidingWindowDurationProperties slidingWindowDurationProperties)
                {
                    if (null == slidingWindowDurationProperties.SlidingWindowDuration)
                    {
                        slidingWindowDurationProperties.SlidingWindowDuration = triggerDefaults.SlidingWindowDuration ?? SlidingWindowDurationDefault;
                    }
                }
            }
        }
    }
}
