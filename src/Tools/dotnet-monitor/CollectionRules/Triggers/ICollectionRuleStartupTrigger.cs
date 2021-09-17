// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Triggers
{
    /// <summary>
    /// Interface that denotes that the trigger is a startup trigger.
    /// </summary>
    /// <remarks>
    /// At this time, only the StartupTrigger should implement this interface.
    /// </remarks>
    internal interface ICollectionRuleStartupTrigger :
        ICollectionRuleTrigger
    {
    }
}
