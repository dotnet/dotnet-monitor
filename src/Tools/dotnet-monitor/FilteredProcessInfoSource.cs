// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    /// <summary>
    /// Wraps an <see cref="IProcessInfoSource"/> based on the provided configuration
    /// and filters the current process from consideration.
    /// </summary>
    internal class FilteredProcessInfoSource : IProcessInfoSource, IAsyncDisposable
    {
        private readonly DiagnosticPortOptions _portOptions;
        private readonly int? _processIdToFilterOut;
        private readonly Guid? _runtimeInstanceCookieToFilterOut;
        private readonly IProcessInfoSource _source;

        public FilteredProcessInfoSource(
            IEnumerable<IProcessInfoSourceCallbacks> callbacks,
            IOptions<DiagnosticPortOptions> portOptions,
            ILogger<ClientProcessInfoSource> clientSourceLogger)
        {
            _portOptions = portOptions.Value;

            DiagnosticPortConnectionMode connectionMode = _portOptions.ConnectionMode.GetValueOrDefault(DiagnosticPortOptionsDefaults.ConnectionMode);

            switch (connectionMode)
            {
                case DiagnosticPortConnectionMode.Connect:
                    _source = new ClientProcessInfoSource(clientSourceLogger);
                    break;
                case DiagnosticPortConnectionMode.Listen:
                    _source = new ServerProcessInfoSource(_portOptions.EndpointName, callbacks);
                    break;
                default:
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Strings.ErrorMessage_UnhandledConnectionMode, connectionMode));
            }

            // Filter out the current process based on the connection mode.
            if (RuntimeInfo.IsDiagnosticsEnabled)
            {
                int pid = Process.GetCurrentProcess().Id;

                // Regardless of connection mode, can use the runtime instance cookie to filter self out.
                try
                {
                    var client = new DiagnosticsClient(pid);
                    Guid runtimeInstanceCookie = client.GetProcessInfo().RuntimeInstanceCookie;
                    if (Guid.Empty != runtimeInstanceCookie)
                    {
                        _runtimeInstanceCookieToFilterOut = runtimeInstanceCookie;
                    }
                }
                catch (Exception)
                {
                }

                // If connecting to runtime instances, filter self out. In listening mode, it's likely
                // that multiple processes have the same PID in multi-container scenarios.
                if (DiagnosticPortConnectionMode.Connect == connectionMode)
                {
                    _processIdToFilterOut = pid;
                }
            }
        }

        public async Task<IEnumerable<IProcessInfo>> GetProcessInfoAsync(CancellationToken token)
        {
            var processInfos = await _source.GetProcessInfoAsync(token);

            // Apply process ID filter
            if (_processIdToFilterOut.HasValue)
            {
                processInfos = processInfos.Where(info => info.ProcessId != _processIdToFilterOut.Value);
            }

            // Apply runtime instance cookie filter
            if (_runtimeInstanceCookieToFilterOut.HasValue)
            {
                processInfos = processInfos.Where(info => info.RuntimeInstanceCookie != _runtimeInstanceCookieToFilterOut.Value);
            }

            return processInfos;
        }

        public async ValueTask DisposeAsync()
        {
            if (_source is IDisposable disposable)
            {
                disposable.Dispose();
            }
            else if (_source is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.ConfigureAwait(false).DisposeAsync();
            }
        }

        public void Start()
        {
            if (_source is ServerProcessInfoSource source)
            {
                source.Start(_portOptions.MaxConnections.GetValueOrDefault(ReversedDiagnosticsServer.MaxAllowedConnections));
            }
        }
    }
}
