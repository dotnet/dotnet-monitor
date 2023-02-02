// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using System;

namespace Microsoft.Diagnostics.Monitoring.TestCommon.Options
{
    internal static partial class CollectionRuleDefaultsOptionsExtensions
    {
        public static RootOptions AddCollectionRuleDefaults(this RootOptions rootOptions, Action<CollectionRuleDefaultsOptions> callback = null)
        {
            CollectionRuleDefaultsOptions settings = new();

            callback?.Invoke(settings);

            rootOptions.CollectionRuleDefaults = settings;
            return rootOptions;
        }

        public static CollectionRuleDefaultsOptions SetLimitsDefaults(this CollectionRuleDefaultsOptions options, int? count = null, TimeSpan? slidingWindowDuration = null, TimeSpan? ruleDuration = null)
        {
            if (null == options.Limits)
            {
                options.Limits = new CollectionRuleLimitsDefaultsOptions();
            }

            options.Limits.ActionCount = count;
            options.Limits.ActionCountSlidingWindowDuration = slidingWindowDuration;
            options.Limits.RuleDuration = ruleDuration;

            return options;
        }

        public static CollectionRuleDefaultsOptions SetTriggerDefaults(this CollectionRuleDefaultsOptions options, int? requestCount = null, int? responseCount = null, string slidingWindowDuration = null)
        {
            if (null == options.Triggers)
            {
                options.Triggers = new CollectionRuleTriggerDefaultsOptions();
            }

            options.Triggers.RequestCount = requestCount;
            options.Triggers.ResponseCount = responseCount;
            options.Triggers.SlidingWindowDuration = slidingWindowDuration != null ? TimeSpan.Parse(slidingWindowDuration) : null;

            return options;
        }

        public static CollectionRuleDefaultsOptions SetActionDefaults(this CollectionRuleDefaultsOptions options, string egress = null)
        {
            if (null == options.Actions)
            {
                options.Actions = new CollectionRuleActionDefaultsOptions();
            }

            options.Actions.Egress = egress;

            return options;
        }
    }
}
