// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

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

        private readonly SemaphoreSlim _activeEndpointsSemaphore = new(1);
        private readonly Dictionary<Guid, AsyncServiceScope> _activeEndpointServiceScopes = new();
        private readonly IServiceScopeFactory _scopeFactory;

        private readonly ChannelReader<EndpointRemovedEventArgs> _pendingRemovalReader;
        private readonly ChannelWriter<EndpointRemovedEventArgs> _pendingRemovalWriter;

        private readonly IEnumerable<IEndpointInfoSourceCallbacks> _callbacks;
        private readonly DiagnosticPortOptions _portOptions;

        private readonly ILogger<ServerEndpointInfoSource> _logger;

        private readonly IServerEndpointTracker _endpointTracker;

        private long _disposalState;

        /// <summary>
        /// Constructs a <see cref="ServerEndpointInfoSource"/> that aggregates diagnostic endpoints
        /// from a reversed diagnostics server at path specified by <paramref name="portOptions"/>.
        /// </summary>
        public ServerEndpointInfoSource(
            IServiceScopeFactory scopeFactory,
            IServerEndpointTracker endpointTracker,
            IOptions<DiagnosticPortOptions> portOptions,
            ILogger<ServerEndpointInfoSource> logger,
            IEnumerable<IEndpointInfoSourceCallbacks> callbacks = null)
        {
            _callbacks = callbacks ?? Enumerable.Empty<IEndpointInfoSourceCallbacks>();
            _portOptions = portOptions.Value;
            _logger = logger;
            _endpointTracker = endpointTracker;
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));

            BoundedChannelOptions channelOptions = new(PendingRemovalChannelCapacity)
            {
                SingleReader = true,
                SingleWriter = true
            };
            Channel<EndpointRemovedEventArgs> pendingRemovalChannel = Channel.CreateBounded<EndpointRemovedEventArgs>(channelOptions);
            _pendingRemovalReader = pendingRemovalChannel.Reader;
            _pendingRemovalWriter = pendingRemovalChannel.Writer;

            _endpointTracker.EndpointRemoved += OnEndpointRemoved;
        }

        public async ValueTask DisposeAsync()
        {
            if (!DisposableHelper.CanDispose(ref _disposalState))
                return;

            // Makes sure the background task is canceled.
            Dispose();

            _endpointTracker.EndpointRemoved -= OnEndpointRemoved;
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
            => await _endpointTracker.GetEndpointInfoAsync(token);

        private void OnEndpointRemoved(object sender, EndpointRemovedEventArgs args)
            => _pendingRemovalWriter.TryWrite(args);

        /// <summary>
        /// Accepts endpoint infos from the reversed diagnostics server.
        /// </summary>
        /// <param name="server">The reversed diagnostics server instance from which connections are accepted.</param>
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
                await _activeEndpointsSemaphore.WaitAsync(token);
                try
                {
                    _activeEndpointServiceScopes.Add(endpointInfo.RuntimeInstanceCookie, scope);
                }
                finally
                {
                    _activeEndpointsSemaphore.Release();
                }

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
                    await _endpointTracker.AddAsync(endpointInfo, token).ConfigureAwait(false);

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

        private async Task NotifyAndRemoveAsync(ReversedDiagnosticsServer server, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                EndpointRemovedEventArgs args = await _pendingRemovalReader.ReadAsync(token);
                IEndpointInfo endpoint = args.Endpoint;
                ServerEndpointState state = args.State;

                List<Exception> exceptions = new();

                AsyncServiceScope serviceScope;
                bool isServiceScopeValid = false;

                if (state == ServerEndpointState.Unresponsive)
                {
                    _logger.EndpointTimeout(endpoint.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture));
                }

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
