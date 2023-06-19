// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.HostingStartup
{
    internal sealed class HostingStartupEndpointInfoSourceCallbacks : IEndpointInfoSourceCallbacks
    {
        private readonly HostingStartupService _hostingStartupService;

        public HostingStartupEndpointInfoSourceCallbacks(HostingStartupService hostingStartupService)
        {
            _hostingStartupService = hostingStartupService;
        }

        Task IEndpointInfoSourceCallbacks.OnAddedEndpointInfoAsync(IEndpointInfo endpointInfo, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        Task IEndpointInfoSourceCallbacks.OnBeforeResumeAsync(IEndpointInfo endpointInfo, CancellationToken cancellationToken)
        {
            return _hostingStartupService.ApplyHostingStartup(endpointInfo, cancellationToken);
        }

        Task IEndpointInfoSourceCallbacks.OnRemovedEndpointInfoAsync(IEndpointInfo endpointInfo, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
