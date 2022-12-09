// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Diagnostics.Monitoring.Tool.TestHostingStartup;
using Microsoft.Diagnostics.Tools.Monitor.Profiler;
using Microsoft.Extensions.DependencyInjection;

[assembly: HostingStartup(typeof(HostingStartup))]

namespace Microsoft.Diagnostics.Monitoring.Tool.TestHostingStartup
{
    public class HostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) =>
            {
                services.AddSingleton<ISharedLibraryInitializer, BuildOutputSharedLibraryInitializer>();
            });
        }
    }
}
