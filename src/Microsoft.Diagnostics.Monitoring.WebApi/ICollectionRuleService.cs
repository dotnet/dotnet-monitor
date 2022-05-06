// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal interface ICollectionRuleService : IHostedService, IAsyncDisposable
    {
        Task ApplyRules(
            IEndpointInfo endpointInfo,
            CancellationToken token);

        Task RemoveRules(
            IEndpointInfo endpointInfo,
            CancellationToken token);

        Dictionary<string, CollectionRuleDescription> GetCollectionRulesDescriptions(IEndpointInfo endpointInfo);
    }
}
