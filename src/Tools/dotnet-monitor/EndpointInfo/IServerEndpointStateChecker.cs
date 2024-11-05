// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System.Threading.Tasks;
using System.Threading;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal enum ServerEndpointState
    {
        Active,
        Unresponsive,
        Error
    }

    internal interface IServerEndpointStateChecker
    {
        Task<ServerEndpointState> GetEndpointStateAsync(IEndpointInfo info, CancellationToken token);
    }
}
