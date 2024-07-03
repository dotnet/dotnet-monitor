// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Triggers
{
    /// <summary>
    /// A collection rule trigger factory interface with untyped options that
    /// provides a proxy to the real trigger factory implementation and verifies
    /// that the passed options are of the correct type.
    /// </summary>
    /// <remarks>
    /// Allows the rest of the collection rule system to not have to understand
    /// the type of the options to pass to the factory.
    /// </remarks>
    internal interface ICollectionRuleTriggerFactoryProxy
    {
        /// <summary>
        /// Executes the underlying factory with the specified parameters, verifying
        /// that the passed options are of the correct type.
        /// </summary>
        ICollectionRuleTrigger Create(IEndpointInfo endpointInfo, Action callback, object? options);
    }
}
