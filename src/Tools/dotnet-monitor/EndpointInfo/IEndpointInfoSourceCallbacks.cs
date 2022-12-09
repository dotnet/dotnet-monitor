// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    /// <summary>
    /// Callback interface for notifications on state changes for an IEndpointInfoSource implementation.
    /// </summary>
    internal interface IEndpointInfoSourceCallbacks
    {
        Task OnBeforeResumeAsync(IEndpointInfo endpointInfo, CancellationToken cancellationToken);

        Task OnAddedEndpointInfoAsync(IEndpointInfo endpointInfo, CancellationToken cancellationToken);

        Task OnRemovedEndpointInfoAsync(IEndpointInfo endpointInfo, CancellationToken cancellationToken);
    }
}
