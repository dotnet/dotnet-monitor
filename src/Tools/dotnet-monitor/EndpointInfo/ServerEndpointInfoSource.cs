// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    /// <summary>
    /// Aggregates diagnostic endpoints that are established at a transport path via a reversed server.
    /// </summary>
    internal sealed class ServerEndpointInfoSource : BackgroundService, IEndpointInfoSourceInternal
    {
        // The amount of time to wait when checking if the a endpoint info should be
        // pruned from the list of endpoint infos. If the runtime doesn't have a viable connection within
        // this time, it will be pruned from the list.
        private static readonly TimeSpan PruneWaitForConnectionTimeout = TimeSpan.FromMilliseconds(250);

        private readonly List<EndpointInfo> _activeEndpoints = new();
        private readonly SemaphoreSlim _activeEndpointsSemaphore = new(1);

        private readonly Queue<IEndpointInfo> _pendingRemovalEndpoints = new();
        private readonly SemaphoreSlim _pendingRemovalEndpointsSemaphore = new(0);

        private readonly CancellationTokenSource _cancellation = new();
        private readonly IEnumerable<IEndpointInfoSourceCallbacks> _callbacks;
        private readonly DiagnosticPortOptions _portOptions;

        /// <summary>
        /// Constructs a <see cref="ServerEndpointInfoSource"/> that aggregates diagnostic endpoints
        /// from a reversed diagnostics server at path specified by <paramref name="portOptions"/>.
        /// </summary>
        public ServerEndpointInfoSource(
            IOptions<DiagnosticPortOptions> portOptions,
            IEnumerable<IEndpointInfoSourceCallbacks> callbacks = null)
        {
            _callbacks = callbacks ?? Enumerable.Empty<IEndpointInfoSourceCallbacks>();
            _portOptions = portOptions.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_portOptions.ConnectionMode == DiagnosticPortConnectionMode.Listen)
            {
                await using ReversedDiagnosticsServer server = new(_portOptions.EndpointName);

                server.Start(_portOptions.MaxConnections.GetValueOrDefault(ReversedDiagnosticsServer.MaxAllowedConnections));

                await Task.WhenAll(
                    ListenAsync(server, stoppingToken),
                    MonitorEndpointsAsync(stoppingToken),
                    NotifyAndRemoveAsync(server, stoppingToken)
                    );
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

        /// <summary>
        /// Accepts endpoint infos from the reversed diagnostics server.
        /// </summary>
        /// <param name="maxConnections">The maximum number of connections the server will support.</param>
        /// <param name="token">The token to monitor for cancellation requests.</param>
        private async Task ListenAsync(ReversedDiagnosticsServer server, CancellationToken token)
        {
            // Continuously accept endpoint infos from the reversed diagnostics server so
            // that <see cref="ReversedDiagnosticsServer.AcceptAsync(CancellationToken)"/>
            // is always awaited in order to to handle new runtime instance connections
            // as well as existing runtime instance reconnections.
            while (!token.IsCancellationRequested)
            {
                try
                {
                    IpcEndpointInfo info = await server.AcceptAsync(token).ConfigureAwait(false);

                    _ = Task.Run(() => ResumeAndQueueEndpointInfo(server, info, token), token);
                }
                catch (OperationCanceledException)
                {
                }
            }
        }

        private async Task ResumeAndQueueEndpointInfo(ReversedDiagnosticsServer server, IpcEndpointInfo info, CancellationToken token)
        {
            try
            {
                EndpointInfo endpointInfo = await EndpointInfo.FromIpcEndpointInfoAsync(info, token);

                foreach (IEndpointInfoSourceCallbacks callback in _callbacks)
                {
                    await callback.OnBeforeResumeAsync(endpointInfo, token).ConfigureAwait(false);
                }

                // Send ResumeRuntime message for runtime instances that connect to the server. This will allow
                // those instances that are configured to pause on start to resume after the diagnostics
                // connection has been made. Instances that are not configured to pause on startup will ignore
                // the command and return success.
                var client = new DiagnosticsClient(info.Endpoint);
                try
                {
                    await client.ResumeRuntimeAsync(token);
                }
                catch (ServerErrorException)
                {
                    // The runtime likely doesn't understand the ResumeRuntime command.
                }

                await _activeEndpointsSemaphore.WaitAsync(token).ConfigureAwait(false);
                try
                {
                    _activeEndpoints.Add(endpointInfo);

                    foreach (IEndpointInfoSourceCallbacks callback in _callbacks)
                    {
                        await callback.OnAddedEndpointInfoAsync(endpointInfo, token).ConfigureAwait(false);
                    }
                }
                finally
                {
                    _activeEndpointsSemaphore.Release();
                }
            }
            catch (Exception)
            {
                server?.RemoveConnection(info.RuntimeInstanceCookie);

                throw;
            }
        }

        private async Task MonitorEndpointsAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(3));

                await PruneEndpointsAsync(validEndpoints: null, token);
            }
        }

        private async Task NotifyAndRemoveAsync(ReversedDiagnosticsServer server, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await _pendingRemovalEndpointsSemaphore.WaitAsync(token).ConfigureAwait(false);

                IEndpointInfo endpoint;
                lock (_pendingRemovalEndpoints)
                {
                   endpoint = _pendingRemovalEndpoints.Dequeue();
                }

                foreach (IEndpointInfoSourceCallbacks callback in _callbacks)
                {
                    await callback.OnRemovedEndpointInfoAsync(endpoint, token).ConfigureAwait(false);
                }

                server.RemoveConnection(endpoint.RuntimeInstanceCookie);
            }
        }

        private async Task PruneEndpointsAsync(List<IEndpointInfo> validEndpoints, CancellationToken token)
        {
            List<IEndpointInfo> endpointsToRemove = new();

            // Prune connections that no longer have an active runtime instance before
            // returning the list of connections.
            await _activeEndpointsSemaphore.WaitAsync(token).ConfigureAwait(false);

            try
            {
                // Check the transport for each endpoint info and remove it if the check fails.
                List<Task<bool>> checkTasks = new();
                foreach (EndpointInfo info in _activeEndpoints)
                {
                    checkTasks.Add(Task.Run(() => CheckEndpointAsync(info, token), token));
                }

                // Wait for all checks to complete
                bool[] results = await Task.WhenAll(checkTasks).ConfigureAwait(false);

                // Remove failed endpoints from active list; record the failed endpoints
                // for removal after releasing the active endpoints semaphore.
                int endpointIndex = 0;
                for (int resultIndex = 0; resultIndex < results.Length; resultIndex++)
                {
                    IEndpointInfo endpoint = _activeEndpoints[endpointIndex];
                    if (results[resultIndex])
                    {
                        validEndpoints?.Add(endpoint);
                        endpointIndex++;
                    }
                    else
                    {
                        _activeEndpoints.RemoveAt(endpointIndex);

                        endpointsToRemove.Add(endpoint);
                    }
                }
            }
            finally
            {
                _activeEndpointsSemaphore.Release();
            }

            if (endpointsToRemove.Count > 0)
            {
                lock (_pendingRemovalEndpoints)
                {
                    foreach (IEndpointInfo endpoint in endpointsToRemove)
                    {
                        _pendingRemovalEndpoints.Enqueue(endpoint);
                    }
                }

                _pendingRemovalEndpointsSemaphore.Release(endpointsToRemove.Count);
            }
        }

        /// <summary>
        /// Tests the endpoint to see if its connection is viable.
        /// </summary>
        private static async Task<bool> CheckEndpointAsync(EndpointInfo info, CancellationToken token)
        {
            using var timeoutSource = new CancellationTokenSource();
            using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(token, timeoutSource.Token);

            try
            {
                timeoutSource.CancelAfter(PruneWaitForConnectionTimeout);

                await info.Endpoint.WaitForConnectionAsync(linkedSource.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested)
            {
                return false;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return false;
            }

            return true;
        }
    }
}
