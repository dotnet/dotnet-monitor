// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    /// <summary>
    /// Wraps an <see cref="IEndpointInfoSource"/> based on the provided configuration
    /// and filters the current process from consideration.
    /// </summary>
    internal class FilteredEndpointInfoSource : IEndpointInfoSourceInternal
    {
        private readonly DiagnosticPortOptions _portOptions;
        private readonly int? _processIdToFilterOut;
        private readonly Guid? _runtimeInstanceCookieToFilterOut;
        private readonly IEndpointInfoSourceInternal _source;

        public FilteredEndpointInfoSource(
            ServerEndpointInfoSource serverEndpointInfoSource,
            IOptions<DiagnosticPortOptions> portOptions,
            ILogger<ClientEndpointInfoSource> clientSourceLogger)
        {
            _portOptions = portOptions.Value;

            DiagnosticPortConnectionMode connectionMode = _portOptions.GetConnectionMode();

            switch (connectionMode)
            {
                case DiagnosticPortConnectionMode.Connect:
                    _source = new ClientEndpointInfoSource(clientSourceLogger);
                    break;
                case DiagnosticPortConnectionMode.Listen:
                    _source = serverEndpointInfoSource;
                    break;
                default:
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, Strings.ErrorMessage_UnhandledConnectionMode, connectionMode));
            }

            // Filter out the current process based on the connection mode.
            if (RuntimeInfo.IsDiagnosticsEnabled)
            {
                int pid = Environment.ProcessId;

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
                catch (Exception ex)
                {
                    clientSourceLogger.RuntimeInstanceCookieFailedToFilterSelf(ex);
                }

                // If connecting to runtime instances, filter self out. In listening mode, it's likely
                // that multiple processes have the same PID in multi-container scenarios.
                if (DiagnosticPortConnectionMode.Connect == connectionMode)
                {
                    _processIdToFilterOut = pid;
                }
            }
        }

        public async Task<IEnumerable<IEndpointInfo>> GetEndpointInfoAsync(CancellationToken token)
        {
            var endpointInfos = await _source.GetEndpointInfoAsync(token);

            // Apply process ID filter
            if (_processIdToFilterOut.HasValue)
            {
                endpointInfos = endpointInfos.Where(info => info.ProcessId != _processIdToFilterOut.Value);
            }

            // Apply runtime instance cookie filter
            if (_runtimeInstanceCookieToFilterOut.HasValue)
            {
                endpointInfos = endpointInfos.Where(info => info.RuntimeInstanceCookie != _runtimeInstanceCookieToFilterOut.Value);
            }

            return endpointInfos;
        }
    }
}
