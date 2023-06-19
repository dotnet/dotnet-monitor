// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Diagnostics.Monitoring.HostingStartup;
using Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing;
using Microsoft.Diagnostics.Tools.Monitor.HostingStartup;
using Microsoft.Diagnostics.Tools.Monitor;
using System;

[assembly: HostingStartup(typeof(HostingStartup))]
namespace Microsoft.Diagnostics.Monitoring.HostingStartup
{
    internal sealed class HostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Add a no-op singleton for easy identification that this hosting startup is active.
                services.AddSingleton<InProcMonitoringSentinelService>();

                if (ToolIdentifiers.IsEnvVarEnabled(InProcessFeaturesIdentifiers.EnvironmentVariables.EnableParameterCapturing))
                {
                    services.AddHostedService<ParameterCapturingService>();
                }
            });
        }
    }
}
