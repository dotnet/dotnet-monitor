// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Triggers;
using Microsoft.Extensions.Options;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.CollectionRuleDefaultsInterfaces
{
    internal sealed class RequestCountsPostConfigure :
        IPostConfigureOptions<CollectionRuleOptions>
    {
        private readonly IOptionsMonitor<CollectionRuleDefaultsOptions> _defaultOptions;
        private readonly ICollectionRuleTriggerOperations _triggerOperations;

        public RequestCountsPostConfigure(
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

                if (null != triggerSettings && typeof(IRequestCountProperties).IsAssignableFrom(triggerSettings.GetType()))
                {
                    if (0 == ((IRequestCountProperties)options.Trigger.Settings).RequestCount && _defaultOptions.CurrentValue.RequestCount.HasValue)
                    {
                        ((IRequestCountProperties)options.Trigger.Settings).RequestCount = _defaultOptions.CurrentValue.RequestCount.Value;
                    }
                }
            }
        }
    }
}
