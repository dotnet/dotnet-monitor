// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class ServerEndpointTrackerV2(IServerEndpointStateChecker endpointChecker, TimeProvider timeProvider, IOptions<DiagnosticPortOptions> portOptions) :
        BackgroundService,
        IServerEndpointTracker
    {
        private record class ActiveEndpoint(IEndpointInfo Endpoint, DateTimeOffset LastContact)
        {
            // LastContact should be mutable
            public DateTimeOffset LastContact { get; set; } = LastContact;
        }

        private static readonly TimeSpan PruningInterval = TimeSpan.FromSeconds(3);
        // Internal for testing
        internal static readonly TimeSpan UnresponsiveGracePeriod = TimeSpan.FromMinutes(1);


        private readonly List<ActiveEndpoint> _activeEndpoints = new();
        private readonly SemaphoreSlim _activeEndpointsSemaphore = new(1);

        private readonly DiagnosticPortOptions _portOptions = portOptions.Value;

        public event EventHandler<EndpointRemovedEventArgs>? EndpointRemoved;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_portOptions.ConnectionMode != DiagnosticPortConnectionMode.Listen)
            {
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(PruningInterval, stoppingToken);

                await PruneEndpointsAsync(stoppingToken);
            }
        }

        public async Task<IEnumerable<IEndpointInfo>> GetEndpointInfoAsync(CancellationToken token)
        {
            await _activeEndpointsSemaphore.WaitAsync(token).ConfigureAwait(false);
            try
            {
                return _activeEndpoints.Select(a => a.Endpoint);
            }
            finally
            {
                _activeEndpointsSemaphore.Release();
            }
        }

        /// <summary>
        ///  Internal for testing. Do not directly call outside of tests or from this class.
        /// </summary>
        internal async Task PruneEndpointsAsync(CancellationToken token)
        {
            await _activeEndpointsSemaphore.WaitAsync(token).ConfigureAwait(false);

            try
            {
                // Check the transport for each endpoint info and remove it if the check fails.
                List<Task<ServerEndpointState>> checkTasks = [];
                foreach (ActiveEndpoint activeEndpoint in _activeEndpoints)
                {
                    checkTasks.Add(Task.Run(() => endpointChecker.GetEndpointStateAsync(activeEndpoint.Endpoint, token), token));
                }

                // Wait for all checks to complete
                ServerEndpointState[] states = await Task.WhenAll(checkTasks).ConfigureAwait(false);

                // Remove failed endpoints from active list; record the failed endpoints
                // for removal after releasing the active endpoints semaphore.
                int endpointIndex = 0;

                void removeEndpoint(IEndpointInfo endpoint, ServerEndpointState state)
                {
                    _activeEndpoints.RemoveAt(endpointIndex);
                    EndpointRemoved?.Invoke(this, new(endpoint, state));
                }

                DateTimeOffset now = timeProvider.GetUtcNow();

                for (int resultIndex = 0; resultIndex < states.Length; resultIndex++)
                {
                    ActiveEndpoint activeEndpoint = _activeEndpoints[endpointIndex];
                    ServerEndpointState state = states[resultIndex];

                    switch (state)
                    {
                        case ServerEndpointState.Active:
                            activeEndpoint.LastContact = now;
                            endpointIndex++;
                            break;

                        case ServerEndpointState.Unresponsive:
                            if (now - activeEndpoint.LastContact > UnresponsiveGracePeriod)
                            {
                                removeEndpoint(activeEndpoint.Endpoint, state);
                            }
                            else
                            {
                                endpointIndex++;
                            }
                            break;

                        case ServerEndpointState.Error:
                            removeEndpoint(activeEndpoint.Endpoint, state);
                            break;
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
                _activeEndpoints.Add(new(endpointInfo, timeProvider.GetUtcNow()));
            }
            finally
            {
                _activeEndpointsSemaphore.Release();
            }
        }
    }
}
