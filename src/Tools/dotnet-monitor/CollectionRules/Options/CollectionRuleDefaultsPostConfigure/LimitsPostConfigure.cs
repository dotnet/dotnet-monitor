// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.CollectionRuleDefaultsInterfaces
{
    internal sealed class LimitsPostConfigure :
        IPostConfigureOptions<CollectionRuleOptions>
    {
        private readonly IOptionsMonitor<CollectionRuleDefaultsOptions> _defaultOptions;

        public LimitsPostConfigure(IOptionsMonitor<CollectionRuleDefaultsOptions> defaultOptions)
        {
            _defaultOptions = defaultOptions;
        }

        public void PostConfigure(string name, CollectionRuleOptions options)
        {
            if (null == options.Limits)
            {
                if (!_defaultOptions.CurrentValue.ActionCount.HasValue
                    && !_defaultOptions.CurrentValue.ActionCountSlidingWindowDuration.HasValue
                    && !_defaultOptions.CurrentValue.RuleDuration.HasValue)
                {
                    return;
                }

                options.Limits = new CollectionRuleLimitsOptions();
            }

            if (null == options.Limits.ActionCount)
            {
                options.Limits.ActionCount = _defaultOptions.CurrentValue.ActionCount ?? CollectionRuleLimitsOptionsDefaults.ActionCount;
            }

            if (null == options.Limits.ActionCountSlidingWindowDuration)
            {
                options.Limits.ActionCountSlidingWindowDuration = _defaultOptions.CurrentValue.ActionCountSlidingWindowDuration;
            }

            if (null == options.Limits.RuleDuration)
            {
                options.Limits.RuleDuration = _defaultOptions.CurrentValue.RuleDuration;
            }
        }
    }
}
