// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class OperationTrackerServiceEndpointInfoSourceCallback : IEndpointInfoSourceCallbacks
    {
        private readonly OperationTrackerService _operationTrackerService;

        public OperationTrackerServiceEndpointInfoSourceCallback(OperationTrackerService operationTrackerService)
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
