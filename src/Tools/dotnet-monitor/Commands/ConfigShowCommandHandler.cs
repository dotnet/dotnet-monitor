﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Linq;

namespace Microsoft.Diagnostics.Tools.Monitor.Commands
{
    internal sealed class ConfigShowCommandHandler
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
            IHost host = HostBuilderHelper.CreateHostBuilder(settings).Build();
            IConfiguration configuration = host.Services.GetRequiredService<IConfiguration>();
            using ConfigurationJsonWriter jsonWriter = new ConfigurationJsonWriter(stream);
            jsonWriter.Write(configuration, full: level == ConfigDisplayLevel.Full, skipNotPresent: false);

            if (showSources)
            {
                WriteConfigurationProviders(stream, configuration);
            }
        }

        private static void WriteConfigurationProviders(Stream stream, IConfiguration configuration)
        {
            var configurationProviders = ((IConfigurationRoot)configuration).Providers.Reverse();

            StreamWriter writer = new StreamWriter(stream);

            writer.WriteLine("Configuration Providers (High to Low Priority):");

            foreach (var provider in configurationProviders)
            {
                writer.WriteLine(" - " + provider.ToString());
            }

            writer.WriteLine();

            writer.Flush();
        }
    }
}
