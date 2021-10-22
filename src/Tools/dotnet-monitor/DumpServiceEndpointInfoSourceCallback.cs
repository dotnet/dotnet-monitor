// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class DumpServiceEndpointInfoSourceCallback : IEndpointInfoSourceCallbacks
    {
        private readonly IDumpService _dumpService;

        public DumpServiceEndpointInfoSourceCallback(IDumpService dumpService)
        {
            _dumpService = dumpService ?? throw new ArgumentNullException(nameof(dumpService));
        }

        public Task OnAddedEndpointInfoAsync(IEndpointInfo endpointInfo, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task OnBeforeResumeAsync(IEndpointInfo endpointInfo, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task OnRemovedEndpointInfoAsync(IEndpointInfo endpointInfo, CancellationToken cancellationToken)
        {
            _dumpService.EndpointRemoved(endpointInfo);

            return Task.CompletedTask;
        }
    }
}
