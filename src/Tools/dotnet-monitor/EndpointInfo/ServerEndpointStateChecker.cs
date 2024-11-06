// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal class ServerEndpointStateChecker(OperationTrackerService? operationTracker) : IServerEndpointStateChecker
    {
        // The amount of time to wait when checking if the a endpoint info is active.
        private static readonly TimeSpan WaitForConnectionTimeout = TimeSpan.FromMilliseconds(250);

        public async Task<ServerEndpointState> GetEndpointStateAsync(IEndpointInfo info, CancellationToken token)
        {
            // If a dump operation is in progress, the runtime is likely to not respond to
            // diagnostic requests. Do not check for responsiveness while the dump operation
            // is in progress.
            if (operationTracker?.IsExecutingOperation(info) == true)
            {
                return ServerEndpointState.Active;
            }

            using var timeoutSource = new CancellationTokenSource();
            using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(token, timeoutSource.Token);

            try
            {
                timeoutSource.CancelAfter(WaitForConnectionTimeout);

                await info.Endpoint.WaitForConnectionAsync(linkedSource.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested)
            {
                return ServerEndpointState.Unresponsive;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return ServerEndpointState.Error;
            }

            return ServerEndpointState.Active;
        }
    }
}
