// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Microsoft.Diagnostics.Monitoring.Tool.UnitTests
{
    internal static class TriggerTestsHelper
    {
        internal static T GetTriggerOptions<T>(IHost host, string ruleName)
        {
            IOptionsMonitor<CollectionRuleOptions> ruleOptionsMonitor = host.Services.GetService<IOptionsMonitor<CollectionRuleOptions>>();
            T options = (T)ruleOptionsMonitor.Get(ruleName).Trigger.Settings;

            return options;
        }
    }
}
