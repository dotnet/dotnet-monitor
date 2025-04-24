// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if BUILDING_OTEL
#nullable enable

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Diagnostics.Tools.Monitor.OpenTelemetry;

internal sealed class OpenTelemetryService : BackgroundService
{
    private readonly ILogger<OpenTelemetryService> _Logger;
    private readonly IOptionsMonitor<ProcessFilterOptions> _ProcessOptions;
    private readonly OpenTelemetryEndpointManager _OpenTelemetryEndpointManager;
    private readonly IDiagnosticServices _DiagnosticServices;

    public OpenTelemetryService(
        ILogger<OpenTelemetryService> logger,
        IOptionsMonitor<ProcessFilterOptions> processOptions,
        OpenTelemetryEndpointManager openTelemetryEndpointManager,
        IDiagnosticServices diagnosticServices)
    {
        Debugger.Launch();

        _Logger = logger;
        _ProcessOptions = processOptions;
        _OpenTelemetryEndpointManager = openTelemetryEndpointManager;
        _DiagnosticServices = diagnosticServices;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var processOptions = _ProcessOptions.Get(name: null);

            if (processOptions.Filters.Count != 1)
            {
                _Logger.LogInformation("DefaultProcess configuration was not found or was invalid OpenTelemetry monitoring will only be enabled in passive mode");

                await WaitForOptionsReloadOrStop(_ProcessOptions, stoppingToken);

                continue;
            }

            IProcessInfo processInfo;
            try
            {
                processInfo = await _DiagnosticServices.GetProcessAsync(processKey: null, stoppingToken);
            }
            catch (Exception e) when (e is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
            {
                // Note: Most likely we failed to resolve the pid. Attempt to do this again.
                await Task.Delay(5000, stoppingToken);
                continue;
            }

            stoppingToken.ThrowIfCancellationRequested();

            _OpenTelemetryEndpointManager.StartListeningToEndpoint(processInfo.EndpointInfo);

            await WaitForOptionsReloadOrStop(_ProcessOptions, stoppingToken);

            _OpenTelemetryEndpointManager.StopListeningToEndpoint(processInfo.EndpointInfo);
        }
    }

    internal static async Task WaitForOptionsReloadOrStop<T>(IOptionsMonitor<T> options, CancellationToken stoppingToken)
    {
        var cts = new TaskCompletionSource();

        using var token = options.OnChange(o => cts.TrySetResult());

        await cts.WithCancellation(stoppingToken);
    }
}
#endif
