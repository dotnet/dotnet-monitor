// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;

namespace Microsoft.Diagnostics.Tools.Monitor.Commands
{
    internal static class ConfigShowCommandHandler
    {
        // Although the "noHttpEgress" parameter is unused, it keeps the entire command parameter set a superset
        // of the "collect" command so that users can take the same arguments from "collect" and use it on "config show"
        // to get the same configuration without telling them to drop specific command line arguments.
        public static void Invoke(string[] urls, string[] metricUrls, bool metrics, string diagnosticPort, bool noAuth, bool tempApiKey, bool noHttpEgress, ConfigDisplayLevel level, bool showSources)
        {
            Write(Console.OpenStandardOutput(), urls, metricUrls, metrics, diagnosticPort, noAuth, tempApiKey, level, showSources);
        }

        public static void Write(Stream stream, string[] urls, string[] metricUrls, bool metrics, string diagnosticPort, bool noAuth, bool tempApiKey, ConfigDisplayLevel level, bool showSources)
        {
            IAuthConfiguration authConfiguration = HostBuilderHelper.CreateAuthConfiguration(noAuth, tempApiKey);
            HostBuilderSettings settings = HostBuilderSettings.CreateMonitor(urls, metricUrls, metrics, diagnosticPort, authConfiguration);
            IHost host = HostBuilderHelper.CreateHostBuilder(settings).ConfigureServices().Build();
            IConfiguration configuration = host.Services.GetRequiredService<IConfiguration>();

            using ConfigurationJsonWriter jsonWriter = new ConfigurationJsonWriter(stream);
            jsonWriter.Write(configuration, full: level == ConfigDisplayLevel.Full, skipNotPresent: false, showSources: showSources, host.Services);
        }

        private static IHostBuilder ConfigureServices(this IHostBuilder builder)
        {
            return builder.ConfigureServices((HostBuilderContext context, IServiceCollection services) =>
            {
                //TODO Many of these service additions should be done through extension methods
                services.AddSingleton(RealSystemClock.Instance);

                services.ConfigureDiagnosticPort(context.Configuration);

                services.ConfigureGlobalCounter(context.Configuration);

                services.ConfigureCollectionRuleDefaults(context.Configuration);

                services.AddSingleton<ServerEndpointInfoSource>();
                services.AddHostedServiceForwarder<ServerEndpointInfoSource>();
                services.AddSingleton<IEndpointInfoSourceCallbacks, OperationTrackerServiceEndpointInfoSourceCallback>();
                services.ConfigureEgress();
                services.ConfigureMetrics(context.Configuration);
                services.ConfigureStorage(context.Configuration);
                services.ConfigureDefaultProcess(context.Configuration);
                services.ConfigureCollectionRules();
            });
        }
    }
}
