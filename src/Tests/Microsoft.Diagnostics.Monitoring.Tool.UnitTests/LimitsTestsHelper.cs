// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    internal static class LimitsTestsHelper
    {
        internal static CollectionRuleLimitsOptions GetLimitsOptions(IHost host, string ruleName)
        {
            IOptionsMonitor<CollectionRuleOptions> ruleOptionsMonitor = host.Services.GetService<IOptionsMonitor<CollectionRuleOptions>>();
            CollectionRuleLimitsOptions options = ruleOptionsMonitor.Get(ruleName).Limits;

            return options;
        }
    }
}
