// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    /// <summary>
    /// Aggregates diagnostic endpoints that are established at a transport path via a reversed server.
    /// </summary>
    internal sealed class ServerEndpointTracker(IServerEndpointChecker endpointChecker) :
        BackgroundService,
        IServerEndpointTracker
    {
        // The amount of time to wait between pruning operations.
        private static readonly TimeSpan PruningInterval = TimeSpan.FromSeconds(3);

        private readonly List<IEndpointInfo> _activeEndpoints = new();
        private readonly SemaphoreSlim _activeEndpointsSemaphore = new(1);

        private readonly CancellationTokenSource _cancellation = new();

        public event EventHandler<EndpointRemovedEventArgs>? EndpointRemoved;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(PruningInterval, stoppingToken);

                await PruneEndpointsAsync(validEndpoints: null, stoppingToken);
            }
        }

        /// <summary>
        /// Gets the list of <see cref="IpcEndpointInfo"/> served from the reversed diagnostics server.
        /// </summary>
        /// <param name="token">The token to monitor for cancellation requests.</param>
        /// <returns>A list of active <see cref="IEndpointInfo"/> instances.</returns>
        public async Task<IEnumerable<IEndpointInfo>> GetEndpointInfoAsync(CancellationToken token)
        {
            using CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(token, _cancellation.Token);

            List<IEndpointInfo> validEndpoints = new();
            await PruneEndpointsAsync(validEndpoints, linkedSource.Token);
            return validEndpoints;
        }


        private async Task PruneEndpointsAsync(List<IEndpointInfo>? validEndpoints, CancellationToken token)
        {
            // Prune connections that no longer have an active runtime instance before
            // returning the list of connections.
            await _activeEndpointsSemaphore.WaitAsync(token).ConfigureAwait(false);

            try
            {
                // Check the transport for each endpoint info and remove it if the check fails.
                List<Task<EndpointRemovalReason?>> checkTasks = new();
                foreach (EndpointInfo info in _activeEndpoints)
                {
                    checkTasks.Add(Task.Run(() => endpointChecker.CheckEndpointAsync(info, token), token));
                }

                // Wait for all checks to complete
                EndpointRemovalReason?[] results = await Task.WhenAll(checkTasks).ConfigureAwait(false);

                // Remove failed endpoints from active list; record the failed endpoints
                // for removal after releasing the active endpoints semaphore.
                int endpointIndex = 0;
                for (int resultIndex = 0; resultIndex < results.Length; resultIndex++)
                {
                    IEndpointInfo endpoint = _activeEndpoints[endpointIndex];
                    EndpointRemovalReason? reason = results[resultIndex];

                    if (!reason.HasValue)
                    {
                        validEndpoints?.Add(endpoint);
                        endpointIndex++;
                    }
                    else
                    {
                        _activeEndpoints.RemoveAt(endpointIndex);
                        EndpointRemoved?.Invoke(this, new(endpoint, reason.Value));
                    }
                }
            }
            finally
            {
                _activeEndpointsSemaphore.Release();
            }
        }

        public async Task AddAsync(IEndpointInfo endpointInfo, CancellationToken token)
        {
            await _activeEndpointsSemaphore.WaitAsync(token).ConfigureAwait(false);
            try
            {
                _activeEndpoints.Add(endpointInfo);
            }
            finally
            {
                _activeEndpointsSemaphore.Release();
            }
        }

        public async Task ClearAsync(CancellationToken token)
        {
            await _activeEndpointsSemaphore.WaitAsync(token).ConfigureAwait(false);
            try
            {
                _activeEndpoints.Clear();
            }
            finally
            {
                _activeEndpointsSemaphore.Release();
            }
        }
    }
}
