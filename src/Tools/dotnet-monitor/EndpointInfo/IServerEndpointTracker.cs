// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal record class EndpointRemovedEventArgs(IEndpointInfo Endpoint, ServerEndpointState State);

    internal interface IServerEndpointTracker
    {
        Task AddAsync(IEndpointInfo endpointInfo, CancellationToken token);
        Task<IEnumerable<IEndpointInfo>> GetEndpointInfoAsync(CancellationToken token);

        event EventHandler<EndpointRemovedEventArgs>? EndpointRemoved;
    }
}
