// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.StartupHook
{
    internal sealed class InProcessFeaturesEndpointInfoSourceCallbacks : IEndpointInfoSourceCallbacks
    {
        private readonly InProcessFeaturesService _inProcessFeaturesService;

        public InProcessFeaturesEndpointInfoSourceCallbacks(InProcessFeaturesService inProcessFeaturesService)
        {
            _inProcessFeaturesService = inProcessFeaturesService;
        }

        Task IEndpointInfoSourceCallbacks.OnAddedEndpointInfoAsync(IEndpointInfo endpointInfo, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        Task IEndpointInfoSourceCallbacks.OnBeforeResumeAsync(IEndpointInfo endpointInfo, CancellationToken cancellationToken)
        {
            return _inProcessFeaturesService.ApplyInProcessFeatures(endpointInfo, cancellationToken);
        }

        Task IEndpointInfoSourceCallbacks.OnRemovedEndpointInfoAsync(IEndpointInfo endpointInfo, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
