// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using Microsoft.Extensions.Options;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.CollectionRuleDefaultsInterfaces
{
    internal sealed class EgressPostConfigure :
        IPostConfigureOptions<CollectionRuleOptions>
    {
        private readonly IOptionsMonitor<CollectionRuleDefaultsOptions> _defaultOptions;

        public EgressPostConfigure(
            IOptionsMonitor<CollectionRuleDefaultsOptions> defaultOptions
            )
        {
            _defaultOptions = defaultOptions;
        }

        public void PostConfigure(string name, CollectionRuleOptions options)
        {
            foreach (var action in options.Actions)
            {
                if (null != action.Settings && action.Settings is IEgressProviderProperties egressProviderProperties)
                {
                    if (string.IsNullOrEmpty(egressProviderProperties.Egress))
                    {
                        egressProviderProperties.Egress = _defaultOptions.CurrentValue.Actions.Egress;
                    }
                }
            }
        }
    }
}
