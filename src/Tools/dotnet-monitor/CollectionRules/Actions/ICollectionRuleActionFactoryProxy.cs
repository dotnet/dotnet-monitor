// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions
{
    /// <summary>
    /// A collection rule action interface with untyped options that
    /// provides a proxy to the real action implementation and verifies
    /// that the passed options are of the correct type.
    /// </summary>
    /// <remarks>
    /// Allows the rest of the collection rule system to not have to understand
    /// the type of the options to pass to the action.
    /// </remarks>
    internal interface ICollectionRuleActionFactoryProxy
    {
        /// <summary>
        /// Executes the underlying action with the specified parameters, verifying
        /// that the passed options are of the correct type.
        /// </summary>
        ICollectionRuleAction Create(IProcessInfo endpointInfo, object? options);
    }
}
