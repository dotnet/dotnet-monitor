// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;

namespace Microsoft.Diagnostics.Tools.Monitor.Commands
{
    internal sealed class ConfigShowCommandHandler
    {
        public static void Invoke(string[] urls, string[] metricUrls, bool metrics, string diagnosticPort, bool noAuth, bool tempApiKey, bool noHttpEgress, ConfigDisplayLevel level)
        {
            Write(Console.OpenStandardOutput(), urls, metricUrls, metrics, diagnosticPort, noAuth, tempApiKey, level);
        }

        public static void Write(Stream stream, string[] urls, string[] metricUrls, bool metrics, string diagnosticPort, bool noAuth, bool tempApiKey, ConfigDisplayLevel level)
        {
            IHost host = HostBuilderHelper.CreateHostBuilder(urls, metricUrls, metrics, diagnosticPort, noAuth, tempApiKey).Build();
            IConfiguration configuration = host.Services.GetRequiredService<IConfiguration>();
            using ConfigurationJsonWriter jsonWriter = new ConfigurationJsonWriter(stream);
            jsonWriter.Write(configuration, full: level == ConfigDisplayLevel.Full, skipNotPresent: false);
        }
    }
}
