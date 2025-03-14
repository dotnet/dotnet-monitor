// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Diagnostics.Tools.Monitor.Auth;
using Microsoft.Diagnostics.Tools.Monitor.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Diagnostics.Monitoring.OpenApiGen
{
    internal sealed class Program
    {
        public static void Main(string[] args)
        {

            HostBuilderSettings settings = HostBuilderSettings.CreateMonitor(
                urls: null,
                metricUrls: null,
                metrics: false,
                diagnosticPort: null,
                startupAuthMode: StartupAuthenticationMode.Deferred,
                userProvidedConfigFilePath: null);

            // Create all of the same services as dotnet-monitor and add
            // OpenAPI generation in order to have it inspect the ASP.NET Core
            // registrations and descriptions.
            IHost host = HostBuilderHelper
                .CreateHostBuilder(settings)
                .ConfigureServices(services =>
                {
                    services.AddOpenApi(options => options.ConfigureMonitorOpenApiGen());
                })
                .Build();
        }
    }
}
