// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if BUILDING_OTEL
#nullable enable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Monitoring.WebApi;

namespace Microsoft.Diagnostics.Tools.Monitor.OpenTelemetry;

internal sealed class OpenTelemetryEndpointInfoSourceCallbacks : IEndpointInfoSourceCallbacks
{
    private readonly OpenTelemetryEndpointManager _openTelemetryManager;

    public OpenTelemetryEndpointInfoSourceCallbacks(OpenTelemetryEndpointManager openTelemetryManager)
    {
        _openTelemetryManager = openTelemetryManager;
    }

    Task IEndpointInfoSourceCallbacks.OnAddedEndpointInfoAsync(IEndpointInfo endpointInfo, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    Task IEndpointInfoSourceCallbacks.OnBeforeResumeAsync(IEndpointInfo endpointInfo, CancellationToken cancellationToken)
    {
        _openTelemetryManager.StartListeningToEndpoint(endpointInfo);
        return Task.CompletedTask;
    }

    Task IEndpointInfoSourceCallbacks.OnRemovedEndpointInfoAsync(IEndpointInfo endpointInfo, CancellationToken cancellationToken)
    {
        _openTelemetryManager.StopListeningToEndpoint(endpointInfo);
        return Task.CompletedTask;
    }
}
#endif
