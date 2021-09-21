// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options.Actions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions
{
    internal sealed class CollectGCDumpActionFactory :
        ICollectionRuleActionFactory<CollectGCDumpOptions>
    {
        public ICollectionRuleAction Create(IEndpointInfo endpointInfo, CollectGCDumpOptions options)
        {
            return new CollectGCDumpAction();
        }

        private sealed class CollectGCDumpAction :
            ICollectionRuleAction
        {
            public Task StartAsync(CancellationToken token)
            {
                throw new NotImplementedException("TODO: Implement");
            }

            public Task<CollectionRuleActionResult> WaitForCompletionAsync(CancellationToken token)
            {
                throw new NotImplementedException("TODO: Implement");
            }
        }
    }
}
