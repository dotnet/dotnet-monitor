// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if BUILDING_OTEL
#nullable enable

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Diagnostics.Tools.Monitor.OpenTelemetry;

public static class OpenTelemetryServiceCollectionExtensions
{
    public static IServiceCollection AddOpenTelemetry(
        this IServiceCollection services)
        => AddOpenTelemetry(services, configurationSectionName: "OpenTelemetry");

    public static IServiceCollection AddOpenTelemetry(
        this IServiceCollection services,
        string configurationSectionName)
    {
        services.ConfigureOpenTelemetry(configurationSectionName);

        services.AddHostedService<OpenTelemetryService>();
        services.AddSingleton<OpenTelemetryEndpointManager>();
        services.AddSingleton<IEndpointInfoSourceCallbacks, OpenTelemetryEndpointInfoSourceCallbacks>();

        return services;
    }
}
#endif
