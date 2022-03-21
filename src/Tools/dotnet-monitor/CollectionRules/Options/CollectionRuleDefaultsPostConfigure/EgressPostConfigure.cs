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
        private readonly ICollectionRuleActionOperations _actionOperations;

        public EgressPostConfigure(
            IOptionsMonitor<CollectionRuleDefaultsOptions> defaultOptions,
            ICollectionRuleActionOperations actionOperations
            )
        {
            _defaultOptions = defaultOptions;
            _actionOperations = actionOperations;
        }

        public void PostConfigure(string name, CollectionRuleOptions options)
        {
            foreach (var action in options.Actions)
            {
                _actionOperations.TryCreateOptions(action.Type, out object actionSettings);

                if (null != actionSettings && typeof(IEgressProviderProperties).IsAssignableFrom(actionSettings.GetType()))
                {
                    if (string.IsNullOrEmpty(((IEgressProviderProperties)action.Settings).Egress))
                    {
                        ((IEgressProviderProperties)action.Settings).Egress = _defaultOptions.CurrentValue.ActionDefaults.Egress;
                    }
                }
            }
        }
    }
}
