// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Triggers;
using System;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Triggers
{
    /// <summary>
    /// Factory for creating a new AspNetRequestCount trigger.
    /// </summary>
    internal sealed class AspNetRequestCountTriggerFactory :
        ICollectionRuleTriggerFactory<AspNetRequestCountOptions>
    {
        /// <inheritdoc/>
        public ICollectionRuleTrigger Create(IProcessInfo processInfo, Action callback, AspNetRequestCountOptions options)
        {
            throw new NotImplementedException("TODO: Implement AspNetRequestCountTrigger.");
        }
    }
}
