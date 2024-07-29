// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi.Models;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    internal interface ICollectionRuleService : IHostedService, IAsyncDisposable
    {
        Dictionary<string, CollectionRuleDescription> GetCollectionRulesDescriptions(IEndpointInfo endpointInfo);

        CollectionRuleDetailedDescription? GetCollectionRuleDetailedDescription(string collectionRuleName, IEndpointInfo endpointInfo);
    }
}
