// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers.EventCounterShortcuts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;

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

        internal static Tuple<string, string> GetProviderAndCounterNames(Type triggerType)
        {
            if (triggerType == typeof(CPUUsageOptions))
            {
                return new Tuple<string, string>(IEventCounterShortcutsConstants.SystemRuntime, IEventCounterShortcutsConstants.CPUUsage);
            }
            else if (triggerType == typeof(GCHeapSizeOptions))
            {
                return new Tuple<string, string>(IEventCounterShortcutsConstants.SystemRuntime, IEventCounterShortcutsConstants.GCHeapSize);
            }
            else if (triggerType == typeof(ThreadpoolQueueLengthOptions))
            {
                return new Tuple<string, string>(IEventCounterShortcutsConstants.SystemRuntime, IEventCounterShortcutsConstants.ThreadpoolQueueLength);
            }

            return new Tuple<string, string>(string.Empty, string.Empty);
        }
    }
}
