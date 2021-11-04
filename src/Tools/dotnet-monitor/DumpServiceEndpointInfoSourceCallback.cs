// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    //TODO Rename this
    internal sealed class DumpServiceEndpointInfoSourceCallback : IEndpointInfoSourceCallbacks
    {
        private readonly OperationTrackerService _operationTrackerService;

        public DumpServiceEndpointInfoSourceCallback(OperationTrackerService operationTrackerService)
        {
            _operationTrackerService = operationTrackerService ?? throw new ArgumentNullException(nameof(operationTrackerService));
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
            _operationTrackerService.EndpointRemoved(endpointInfo);

            return Task.CompletedTask;
        }
    }
}
