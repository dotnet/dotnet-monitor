// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Tool.UnitTests.CollectionRules.Triggers;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;

namespace Microsoft.Diagnostics.Monitoring.TestCommon.Options
{
    internal static class CollectionRuleOptionsExtensions
    {
        public static CollectionRuleOptions SetManualTrigger(this CollectionRuleOptions options)
        {
            return options.SetTrigger(ManualTrigger.TriggerName);
        }
    }
}
