// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Triggers
{
    /// <summary>
    /// Interface that the trigger factory should implement for triggers without options.
    /// </summary>
    internal interface ICollectionRuleTriggerFactory
    {
        /// <summary>
        /// Creates a new instance of the associated trigger.
        /// </summary>
        ICollectionRuleTrigger Create(IEndpointInfo endpointInfo, Action callback);
    }

    /// <summary>
    /// Interface that the trigger factory should implement for triggers with options.
    /// </summary>
    internal interface ICollectionRuleTriggerFactory<TOptions>
    {
        /// <summary>
        /// Creates a new instance of the associated trigger.
        /// </summary>
        ICollectionRuleTrigger Create(IEndpointInfo endpointInfo, Action callback, TOptions options);
    }
}
