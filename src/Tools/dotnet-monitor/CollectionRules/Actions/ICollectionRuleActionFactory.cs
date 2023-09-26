// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions
{
    internal interface ICollectionRuleActionFactory<TOptions>
    {
        ICollectionRuleAction Create(IProcessInfo endpointInfo, TOptions options);
    }
}
