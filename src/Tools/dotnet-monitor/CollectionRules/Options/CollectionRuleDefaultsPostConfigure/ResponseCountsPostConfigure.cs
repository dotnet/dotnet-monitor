// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Triggers;
using Microsoft.Extensions.Options;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.CollectionRuleDefaultsInterfaces
{
    internal sealed class ResponseCountsPostConfigure :
        IPostConfigureOptions<CollectionRuleOptions>
    {
        private readonly IOptionsMonitor<CollectionRuleDefaultsOptions> _defaultOptions;
        private readonly ICollectionRuleTriggerOperations _triggerOperations;

        public ResponseCountsPostConfigure(
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

                if (null != triggerSettings && triggerSettings.GetType() == typeof(AspNetResponseStatusOptions))
                {
                    if (0 == ((AspNetResponseStatusOptions)options.Trigger.Settings).ResponseCount && _defaultOptions.CurrentValue.TriggerDefaults.ResponseCount.HasValue)
                    {
                        ((AspNetResponseStatusOptions)options.Trigger.Settings).ResponseCount = _defaultOptions.CurrentValue.TriggerDefaults.ResponseCount.Value;
                    }
                }
            }
        }
    }
}
