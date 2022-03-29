// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.EventPipe;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal interface ICollectionRuleService : IHostedService, IAsyncDisposable
    {
        public Task ApplyRules(
            IEndpointInfo endpointInfo,
            CancellationToken token);

        public Task RemoveRules(
            IEndpointInfo endpointInfo,
            CancellationToken token);

        public Dictionary<string, Models.CollectionRules> GetCollectionRulesState(IEndpointInfo endpointInfo);
    }
}
