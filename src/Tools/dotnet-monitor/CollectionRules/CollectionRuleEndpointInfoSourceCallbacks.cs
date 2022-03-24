// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.CollectionRules
{
    internal class CollectionRuleEndpointInfoSourceCallbacks :
        IEndpointInfoSourceCallbacks
    {
        private readonly ICollectionRuleService _service;

        public CollectionRuleEndpointInfoSourceCallbacks(ICollectionRuleService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        public Task OnAddedEndpointInfoAsync(IEndpointInfo endpointInfo, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task OnBeforeResumeAsync(IEndpointInfo endpointInfo, CancellationToken cancellationToken)
        {
            return _service.ApplyRules(endpointInfo, cancellationToken);
        }

        public Task OnRemovedEndpointInfoAsync(IEndpointInfo endpointInfo, CancellationToken cancellationToken)
        {
            return _service.RemoveRules(endpointInfo, cancellationToken);
        }
    }
}
