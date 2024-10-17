// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System.Threading.Tasks;
using System.Threading;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal enum EndpointRemovalReason
    {
        Unknown,
        Timeout
    }

    internal interface IServerEndpointChecker
    {

        /// <summary>
        /// Tests the endpoint to see if it should be removed. If it should, the reason why is returned.
        /// </summary>
        Task<EndpointRemovalReason?> CheckEndpointAsync(IEndpointInfo info, CancellationToken token);
    }
}
