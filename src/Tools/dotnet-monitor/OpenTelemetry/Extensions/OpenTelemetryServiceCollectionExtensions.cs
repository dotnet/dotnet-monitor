// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Diagnostics.Tools.Monitor.OpenTelemetry;

public static class OpenTelemetryServiceCollectionExtensions
{
    public static IServiceCollection ConfigureOpenTelemetry(
        this IServiceCollection services)
        => ConfigureOpenTelemetry(services, configurationSectionName: "OpenTelemetry");

    public static IServiceCollection ConfigureOpenTelemetry(
        this IServiceCollection services,
        string configurationSectionName)
    {
        if (string.IsNullOrEmpty(configurationSectionName))
        {
            throw new ArgumentNullException(nameof(configurationSectionName));
        }

        services.AddOptions();

        services.TryAddSingleton<IOptionsFactory<OpenTelemetryOptions>>(sp =>
        {
            IConfiguration config = sp.GetRequiredService<IConfiguration>();

            return new OpenTelemetryOptionsFactory(
                config.GetSection(configurationSectionName),
                sp.GetServices<IConfigureOptions<OpenTelemetryOptions>>(),
                sp.GetServices<IPostConfigureOptions<OpenTelemetryOptions>>(),
                sp.GetServices<IValidateOptions<OpenTelemetryOptions>>());
        });

        services.AddSingleton<IValidateOptions<OpenTelemetryOptions>, DataAnnotationValidateOptions<OpenTelemetryOptions>>();

        services.AddHostedService<OpenTelemetryService>();
        services.AddSingleton<OpenTelemetryEndpointManager>();
        services.AddSingleton<IEndpointInfoSourceCallbacks, OpenTelemetryEndpointInfoSourceCallbacks>();

        return services;
    }
}