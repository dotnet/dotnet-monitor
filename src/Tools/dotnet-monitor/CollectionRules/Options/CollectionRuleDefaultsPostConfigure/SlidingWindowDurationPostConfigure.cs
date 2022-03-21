// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Triggers;
using Microsoft.Extensions.Options;
using System;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.CollectionRuleDefaultsInterfaces
{
    internal sealed class SlidingWindowDurationPostConfigure :
        IPostConfigureOptions<CollectionRuleOptions>
    {
        private readonly IOptionsMonitor<CollectionRuleDefaultsOptions> _defaultOptions;
        private readonly ICollectionRuleTriggerOperations _triggerOperations;
        private static readonly TimeSpan SlidingWindowDurationDefault = TimeSpan.Parse(TriggerOptionsConstants.SlidingWindowDuration_Default);

        public SlidingWindowDurationPostConfigure(
            IOptionsMonitor<CollectionRuleDefaultsOptions> defaultOptions,
            ICollectionRuleTriggerOperations triggerOperations
            )
        {
            _defaultOptions = defaultOptions;
            _triggerOperations = triggerOperations;
        }

        public void PostConfigure(string name, CollectionRuleOptions options)
        {
            if (null != options.Trigger)
            {
                _triggerOperations.TryCreateOptions(options.Trigger.Type, out object triggerSettings);

                if (null != triggerSettings && typeof(ISlidingWindowDurationProperties).IsAssignableFrom(triggerSettings.GetType()))
                {
                    if (null == ((ISlidingWindowDurationProperties)options.Trigger.Settings).SlidingWindowDuration)
                    {
                        ((ISlidingWindowDurationProperties)options.Trigger.Settings).SlidingWindowDuration = _defaultOptions.CurrentValue.TriggerDefaults.SlidingWindowDuration ?? SlidingWindowDurationDefault;
                    }
                }
            }
        }
    }
}
