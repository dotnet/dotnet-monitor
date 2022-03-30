// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.CollectionRuleDefaultsInterfaces
{
    internal sealed class RequestCountsPostConfigure :
        IPostConfigureOptions<CollectionRuleOptions>
    {
        private readonly IOptionsMonitor<CollectionRuleDefaultsOptions> _defaultOptions;

        public RequestCountsPostConfigure(
            IOptionsMonitor<CollectionRuleDefaultsOptions> defaultOptions
            )
        {
            _defaultOptions = defaultOptions;
        }

        public void PostConfigure(string name, CollectionRuleOptions options)
        {
            if (null != options.Trigger)
            {
                if (null != options.Trigger.Settings && options.Trigger.Settings is IRequestCountProperties requestCountProperties)
                {
                    if (0 == requestCountProperties.RequestCount && _defaultOptions.CurrentValue.Triggers.RequestCount.HasValue)
                    {
                        requestCountProperties.RequestCount = _defaultOptions.CurrentValue.Triggers.RequestCount.Value;
                    }
                }
            }
        }
    }
}
