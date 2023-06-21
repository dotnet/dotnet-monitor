// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Diagnostics.Monitoring.HostingStartup;
using Microsoft.Diagnostics.Monitoring.HostingStartup.ParameterCapturing;
using Microsoft.Diagnostics.Tools.Monitor.HostingStartup;
using Microsoft.Diagnostics.Tools.Monitor;
using System.Threading;

[assembly: HostingStartup(typeof(HostingStartup))]
namespace Microsoft.Diagnostics.Monitoring.HostingStartup
{
    internal sealed class HostingStartup : IHostingStartup
    {
        public static int InvocationCount;

        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Keep track of how many times this hosting startup has been invoked for easy
                // validation in tests.
                Interlocked.Increment(ref InvocationCount);

                if (ToolIdentifiers.IsEnvVarEnabled(InProcessFeaturesIdentifiers.EnvironmentVariables.EnableParameterCapturing))
                {
                    services.AddHostedService<ParameterCapturingService>();
                }
            });
        }
    }
}
