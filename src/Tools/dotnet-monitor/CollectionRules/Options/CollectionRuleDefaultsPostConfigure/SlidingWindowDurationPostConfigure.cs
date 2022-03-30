// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers;
using Microsoft.Extensions.Options;
using System;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.CollectionRuleDefaultsInterfaces
{
    internal sealed class SlidingWindowDurationPostConfigure :
        IPostConfigureOptions<CollectionRuleOptions>
    {
        private readonly IOptionsMonitor<CollectionRuleDefaultsOptions> _defaultOptions;
        private static readonly TimeSpan SlidingWindowDurationDefault = TimeSpan.Parse(TriggerOptionsConstants.SlidingWindowDuration_Default);

        public SlidingWindowDurationPostConfigure(
            IOptionsMonitor<CollectionRuleDefaultsOptions> defaultOptions
            )
        {
            _defaultOptions = defaultOptions;
        }

        public void PostConfigure(string name, CollectionRuleOptions options)
        {
            if (null != options.Trigger)
            {
                if (null != options.Trigger.Settings && options.Trigger.Settings is ISlidingWindowDurationProperties slidingWindowDurationProperties)
                {
                    if (null == slidingWindowDurationProperties.SlidingWindowDuration)
                    {
                        slidingWindowDurationProperties.SlidingWindowDuration = _defaultOptions.CurrentValue.Triggers.SlidingWindowDuration ?? SlidingWindowDurationDefault;
                    }
                }
            }
        }
    }
}
