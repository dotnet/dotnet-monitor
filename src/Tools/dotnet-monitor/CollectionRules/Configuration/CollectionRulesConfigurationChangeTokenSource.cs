// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Extensions.Options;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Configuration
{
    /// <summary>
    /// Raises a notification that the CollectionRuleOptions may have changed.
    /// </summary>
    internal sealed class CollectionRulesConfigurationChangeTokenSource :
        ConfigurationChangeTokenSource<CollectionRuleOptions>
    {
        public CollectionRulesConfigurationChangeTokenSource(CollectionRulesConfigurationProvider provider)
            : base(provider.Configuration)
        {
        }
    }
}
