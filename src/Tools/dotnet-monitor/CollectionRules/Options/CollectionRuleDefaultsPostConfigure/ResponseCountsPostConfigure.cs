// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers;
using Microsoft.Extensions.Options;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.CollectionRuleDefaultsInterfaces
{
    internal sealed class ResponseCountsPostConfigure :
        IPostConfigureOptions<CollectionRuleOptions>
    {
        private readonly IOptionsMonitor<CollectionRuleDefaultsOptions> _defaultOptions;

        public ResponseCountsPostConfigure(
            IOptionsMonitor<CollectionRuleDefaultsOptions> defaultOptions
            )
        {
            _defaultOptions = defaultOptions;
        }

        public void PostConfigure(string name, CollectionRuleOptions options)
        {
            if (null != options.Trigger)
            {
                if (null != options.Trigger.Settings && options.Trigger.Settings is AspNetResponseStatusOptions responseStatusProperties)
                {
                    if (0 == responseStatusProperties.ResponseCount && _defaultOptions.CurrentValue.Triggers.ResponseCount.HasValue)
                    {
                        responseStatusProperties.ResponseCount = _defaultOptions.CurrentValue.Triggers.ResponseCount.Value;
                    }
                }
            }
        }
    }
}
