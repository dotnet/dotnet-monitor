// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal static class KeyValueLogScopeExtensions
    {
        public static void AddCollectionRuleProcessInfo(this KeyValueLogScope scope, IProcessInfo processInfo)
        {
            scope.Values.Add(
                "TargetProcessId",
                processInfo.ProcessId.ToString(CultureInfo.InvariantCulture));
            scope.Values.Add(
                "TargetRuntimeInstanceCookie",
                processInfo.RuntimeInstanceCookie.ToString("N"));
        }

        public static void AddCollectionRuleName(this KeyValueLogScope scope, string ruleName)
        {
            scope.Values.Add("CollectionRuleName", ruleName);
        }

        public static void AddCollectionRuleTrigger(this KeyValueLogScope scope, string triggerType)
        {
            scope.Values.Add("CollectionRuleTriggerType", triggerType);
        }

        public static void AddCollectionRuleAction(this KeyValueLogScope scope, string actionType, int actionIndex)
        {
            scope.Values.Add("CollectionRuleActionType", actionType);
            scope.Values.Add("CollectionRuleActionIndex", actionIndex);
        }
    }
}
