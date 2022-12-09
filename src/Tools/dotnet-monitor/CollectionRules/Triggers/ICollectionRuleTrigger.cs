// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Triggers
{
    /// <summary>
    /// Interface implemented by collection rule triggers.
    /// </summary>
    internal interface ICollectionRuleTrigger
    {
        /// <summary>
        /// Starts the collection rule trigger.
        /// </summary>
        Task StartAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Stops the collection rule trigger.
        /// </summary>
        Task StopAsync(CancellationToken cancellationToken);
    }
}
