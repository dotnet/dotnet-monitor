// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    /// <summary>
    /// Aggregates diagnostic endpoints that are established at a transport path via a reversed server.
    /// </summary>
    internal sealed class ServerEndpointInfoSource :
        BackgroundService,
        IEndpointInfoSourceInternal,
        IAsyncDisposable
    {
        // The number of items that the pending removal channel will hold before forcing
        // the writer to wait for capacity to be available.
        private const int PendingRemovalChannelCapacity = 1000;

        // The amount of time to wait when checking if the a endpoint info should be
        // pruned from the list of endpoint infos. If the runtime doesn't have a viable connection within
        // this time, it will be pruned from the list.
        private static readonly TimeSpan PruneWaitForConnectionTimeout = TimeSpan.FromMilliseconds(250);

        // The amount of time to wait between pruning operations.
        private static readonly TimeSpan PruningInterval = TimeSpan.FromSeconds(3);

        private readonly List<EndpointInfo> _activeEndpoints = new();
        private readonly SemaphoreSlim _activeEndpointsSemaphore = new(1);
        private readonly Dictionary<Guid, AsyncServiceScope> _activeEndpointServiceScopes = new();
        private readonly IServiceScopeFactory _scopeFactory;

        private readonly ChannelReader<IEndpointInfo> _pendingRemovalReader;
        private readonly ChannelWriter<IEndpointInfo> _pendingRemovalWriter;

        private readonly CancellationTokenSource _cancellation = new();
        private readonly IEnumerable<IEndpointInfoSourceCallbacks> _callbacks;
        private readonly DiagnosticPortOptions _portOptions;

        private readonly OperationTrackerService _operationTrackerService;
        private readonly ILogger<ServerEndpointInfoSource> _logger;

        private long _disposalState;

        /// <summary>
        /// Constructs a <see cref="ServerEndpointInfoSource"/> that aggregates diagnostic endpoints
        /// from a reversed diagnostics server at path specified by <paramref name="portOptions"/>.
        /// </summary>
        public ServerEndpointInfoSource(
            IServiceScopeFactory scopeFactory,
            IOptions<DiagnosticPortOptions> portOptions,
            IEnumerable<IEndpointInfoSourceCallbacks> callbacks = null,
            OperationTrackerService operationTrackerService = null,
            ILogger<ServerEndpointInfoSource> logger = null)
        {
            _callbacks = callbacks ?? Enumerable.Empty<IEndpointInfoSourceCallbacks>();
            _operationTrackerService = operationTrackerService;
            _portOptions = portOptions.Value;
            _logger = logger;
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));

            BoundedChannelOptions channelOptions = new(PendingRemovalChannelCapacity)
            {
                SingleReader = true,
                SingleWriter = true
            };
            Channel<IEndpointInfo> pendingRemovalChannel = Channel.CreateBounded<IEndpointInfo>(channelOptions);
            _pendingRemovalReader = pendingRemovalChannel.Reader;
            _pendingRemovalWriter = pendingRemovalChannel.Writer;
        }

        public async ValueTask DisposeAsync()
        {
            if (!DisposableHelper.CanDispose(ref _disposalState))
                return;

            // Makes sure the background task is canceled.
            Dispose();

            _pendingRemovalWriter.TryComplete();

            // Wait for background services to complete.
            await ExecuteTask.SafeAwait().WaitAsync(CancellationToken.None);

            List<AsyncServiceScope> serviceScopes;

            await _activeEndpointsSemaphore.WaitAsync(CancellationToken.None);
            try
            {
                serviceScopes = new List<AsyncServiceScope>(_activeEndpointServiceScopes.Values);
                _activeEndpointServiceScopes.Clear();
            }
            finally
            {
                _activeEndpointsSemaphore.Release();
            }

            // Only dispose services and not notify of connection disconnects. Don't want services to spend
            // time saving off state, completing sessions, etc but they should aggressively (yet correctly)
            // stop their operations at this point.
            foreach (AsyncServiceScope serviceScope in serviceScopes)
            {
                try
                {
                    await serviceScope.DisposeAsync();
                }
                catch
                {
                }
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_portOptions.ConnectionMode == DiagnosticPortConnectionMode.Listen)
            {
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    if (_portOptions.GetDeleteEndpointOnStartup() &&
                        File.Exists(_portOptions.EndpointName))
                    {
                        // In some circumstances stale files from previous instances of dotnet-monitor cause
                        // the new instance to fail binding. We need to delete the file in this situation.
                        try
                        {
                            _logger.DiagnosticPortDeleteAttempt(_portOptions.EndpointName);
                            File.Delete(_portOptions.EndpointName);
                        }
                        catch (Exception ex)
                        {
                            _logger.DiagnosticPortDeleteFailed(_portOptions.EndpointName, ex);
                        }
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(_portOptions.EndpointName));
                }

                await using ReversedDiagnosticsServer server = new(_portOptions.EndpointName);

                server.Start(_portOptions.MaxConnections.GetValueOrDefault(ReversedDiagnosticsServer.MaxAllowedConnections));

                using var _ = SetupDiagnosticPortWatcher();

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

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await ResumeAndQueueEndpointInfo(server, info, token).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            _logger.EndpointInitializationFailed(info.ProcessId, ex);
                        }
                    }, token);
                }
                catch (OperationCanceledException)
                {
                }
            }
        }

        private async Task ResumeAndQueueEndpointInfo(ReversedDiagnosticsServer server, IpcEndpointInfo info, CancellationToken token)
        {
            List<Exception> exceptions = new();
            try
            {
                // Create the process service scope
                AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();

                EndpointInfo endpointInfo = await EndpointInfo.FromIpcEndpointInfoAsync(info, scope.ServiceProvider, token);
                _activeEndpointServiceScopes.Add(endpointInfo.RuntimeInstanceCookie, scope);

                // Initialize endpoint information within the service scope
                endpointInfo.ServiceProvider.GetRequiredService<ScopedEndpointInfo>().Set(endpointInfo);

                foreach (IEndpointInfoSourceCallbacks callback in _callbacks)
                {
                    try
                    {
                        await callback.OnBeforeResumeAsync(endpointInfo, token).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                }

                foreach (IDiagnosticLifetimeService lifetimeService in endpointInfo.ServiceProvider.GetServices<IDiagnosticLifetimeService>())
                {
                    try
                    {
                        await lifetimeService.StartAsync(token);
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
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
                        try
                        {
                            await callback.OnAddedEndpointInfoAsync(endpointInfo, token).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            exceptions.Add(ex);
                        }
                    }
                }
                finally
                {
                    _activeEndpointsSemaphore.Release();
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }

            if (exceptions.Count > 0)
            {
                throw new AggregateException(exceptions);
            }
        }

        private async Task MonitorEndpointsAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(PruningInterval, token);

                await PruneEndpointsAsync(validEndpoints: null, token);
            }
        }

        private async Task NotifyAndRemoveAsync(ReversedDiagnosticsServer server, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                IEndpointInfo endpoint = await _pendingRemovalReader.ReadAsync(token);

                List<Exception> exceptions = new();

                AsyncServiceScope serviceScope;
                bool isServiceScopeValid = false;

                await _activeEndpointsSemaphore.WaitAsync(token);
                try
                {
                    isServiceScopeValid = _activeEndpointServiceScopes.Remove(endpoint.RuntimeInstanceCookie, out serviceScope);
                }
                finally
                {
                    _activeEndpointsSemaphore.Release();
                }

                if (isServiceScopeValid)
                {
                    foreach (IDiagnosticLifetimeService lifetimeService in serviceScope.ServiceProvider.GetServices<IDiagnosticLifetimeService>().Reverse())
                    {
                        try
                        {
                            await lifetimeService.StopAsync(token);
                        }
                        catch (Exception ex)
                        {
                            exceptions.Add(ex);
                        }
                    }
                }

                foreach (IEndpointInfoSourceCallbacks callback in _callbacks.Reverse())
                {
                    try
                    {
                        await callback.OnRemovedEndpointInfoAsync(endpoint, token).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                }

                if (isServiceScopeValid)
                {
                    try
                    {
                        await serviceScope.DisposeAsync();
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                }

                if (exceptions.Count > 0)
                {
                    _logger.EndpointRemovalFailed(endpoint.ProcessId, new AggregateException(exceptions));
                }

                server.RemoveConnection(endpoint.RuntimeInstanceCookie);
            }
        }

        private async Task PruneEndpointsAsync(List<IEndpointInfo> validEndpoints, CancellationToken token)
        {
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

                        await _pendingRemovalWriter.WriteAsync(endpoint, token);
                    }
                }
            }
            finally
            {
                _activeEndpointsSemaphore.Release();
            }
        }

        /// <summary>
        /// Tests the endpoint to see if its connection is viable.
        /// </summary>
        private async Task<bool> CheckEndpointAsync(EndpointInfo info, CancellationToken token)
        {
            // If a dump operation is in progress, the runtime is likely to not respond to
            // diagnostic requests. Do not check for responsiveness while the dump operation
            // is in progress.
            if (_operationTrackerService?.IsExecutingOperation(info) == true)
            {
                return true;
            }

            using var timeoutSource = new CancellationTokenSource();
            using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(token, timeoutSource.Token);

            try
            {
                timeoutSource.CancelAfter(PruneWaitForConnectionTimeout);

                await info.Endpoint.WaitForConnectionAsync(linkedSource.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested)
            {
                _logger?.EndpointTimeout(info.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture));
                return false;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return false;
            }

            return true;
        }

        private IDisposable SetupDiagnosticPortWatcher()
        {
            // If running on Windows, a named pipe is used so there is no need to watch it.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return null;
            }

            FileSystemWatcher watcher = null;
            try
            {
                watcher = new(Path.GetDirectoryName(_portOptions.EndpointName));
                void onDiagnosticPortAltered()
                {
                    _logger.DiagnosticPortAlteredWhileInUse(_portOptions.EndpointName);
                    try
                    {
                        watcher.EnableRaisingEvents = false;
                    }
                    catch
                    {
                    }
                }

                watcher.Filter = Path.GetFileName(_portOptions.EndpointName);
                watcher.NotifyFilter = NotifyFilters.FileName;
                watcher.Deleted += (_, _) => onDiagnosticPortAltered();
                watcher.Renamed += (_, _) => onDiagnosticPortAltered();
                watcher.Error += (object _, ErrorEventArgs e) => _logger.DiagnosticPortWatchingFailed(_portOptions.EndpointName, e.GetException());
                watcher.EnableRaisingEvents = true;

                return watcher;
            }
            catch (Exception ex)
            {
                _logger.DiagnosticPortWatchingFailed(_portOptions.EndpointName, ex);
                watcher?.Dispose();
            }

            return null;
        }
    }
}
