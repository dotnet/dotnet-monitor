// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if BUILDING_OTEL
#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Configuration;

namespace Microsoft.Diagnostics.Tools.Monitor.OpenTelemetry;

internal sealed class OpenTelemetryEndpointManager : IAsyncDisposable
{
    private readonly ILoggerFactory _LoggerFactory;
    private readonly ILogger<OpenTelemetryEndpointManager> _Logger;
    private readonly IOptionsMonitor<OpenTelemetryOptions> _OpenTelemetryOptions;
    private readonly Dictionary<int, OpenTelemetryEndpointListener> _Endpoints = new();

    public OpenTelemetryEndpointManager(
        ILoggerFactory loggerFactory,
        IOptionsMonitor<OpenTelemetryOptions> openTelemetryOptions)
    {
        _LoggerFactory = loggerFactory;
        _OpenTelemetryOptions = openTelemetryOptions;

        _Logger = loggerFactory.CreateLogger<OpenTelemetryEndpointManager>();
    }

    public void StartListeningToEndpoint(IEndpointInfo endpointInfo)
    {
        OpenTelemetryEndpointListener endpointListener;

        lock (_Endpoints)
        {
            if (_Endpoints.ContainsKey(endpointInfo.ProcessId))
            {
                _Logger.LogWarning("Process {ProcessId} connected but is already subscribed", endpointInfo.ProcessId);
                return;
            }

            _Logger.LogInformation("Process {ProcessId} connected", endpointInfo.ProcessId);

            endpointListener = new OpenTelemetryEndpointListener(
                _LoggerFactory,
                _OpenTelemetryOptions,
                endpointInfo);

            _Endpoints[endpointInfo.ProcessId] = endpointListener;
        }

        endpointListener.StartListening();
    }

    public async Task StopListeningToEndpointAsync(IEndpointInfo endpointInfo)
    {
        OpenTelemetryEndpointListener? endpointListener;

        lock (_Endpoints)
        {
            if (!_Endpoints.TryGetValue(endpointInfo.ProcessId, out endpointListener))
            {
                _Logger.LogWarning("Process {ProcessId} disconnected but a subscription could not be found", endpointInfo.ProcessId);
                return;
            }

            _Logger.LogInformation("Process {ProcessId} disconnected", endpointInfo.ProcessId);

            _Endpoints.Remove(endpointInfo.ProcessId);
        }

        await endpointListener.StopListeningAsync();
    }

    public async ValueTask DisposeAsync()
    {
        List<OpenTelemetryEndpointListener> listeners;

        lock (_Endpoints)
        {
            listeners = new List<OpenTelemetryEndpointListener>(_Endpoints.Values);
            _Endpoints.Clear();
        }

        foreach (OpenTelemetryEndpointListener listener in listeners)
        {
            await listener.StopListeningAsync();
        }
    }
}
#endif
