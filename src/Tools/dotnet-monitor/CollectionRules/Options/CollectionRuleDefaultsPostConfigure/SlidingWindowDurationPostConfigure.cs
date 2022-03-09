// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
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

                if (null != triggerSettings && typeof(SlidingWindowDurations).IsAssignableFrom(triggerSettings.GetType()))
                {
                    if (null == ((SlidingWindowDurations)options.Trigger.Settings).SlidingWindowDuration)
                    {
                        ((SlidingWindowDurations)options.Trigger.Settings).SlidingWindowDuration = _defaultOptions.CurrentValue.SlidingWindowDuration ?? TimeSpan.Parse(TriggerOptionsConstants.SlidingWindowDuration_Default); // Do we want to set the default here or let it be handled later?
                    }
                }
            }
        }
    }
}
