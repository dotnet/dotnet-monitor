// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;

namespace Microsoft.Diagnostics.Tools.Monitor.Commands
{
    internal sealed class ConfigShowCommandHandler
    {
        // Although the "noHttpEgress" parameter is unused, it keeps the entire command parameter set a superset
        // of the "collect" command so that users can take the same arguments from "collect" and use it on "config show"
        // to get the same configuration without telling them to drop specific command line arguments.
        public static void Invoke(string[]? urls, string[]? metricUrls, bool metrics, string? diagnosticPort, bool noAuth, bool tempApiKey, bool noHttpEgress, FileInfo? configurationFilePath, ConfigDisplayLevel level, bool showSources)
        {
            Stream stream = Console.OpenStandardOutput();

            using StreamWriter writer = new(stream, EncodingCache.UTF8NoBOMNoThrow, 1024, leaveOpen: true);
            writer.WriteLine(ExperienceSurvey.ExperienceSurveyMessage);
            writer.WriteLine();
            writer.Flush();

            Write(stream, urls, metricUrls, metrics, diagnosticPort, noAuth, tempApiKey, configurationFilePath, level, showSources);
        }

        public static void Write(Stream stream, string[]? urls, string[]? metricUrls, bool metrics, string? diagnosticPort, bool noAuth, bool tempApiKey, FileInfo? configurationFilePath, ConfigDisplayLevel level, bool showSources)
        {
            StartupAuthenticationMode startupAuthMode = HostBuilderHelper.GetStartupAuthenticationMode(noAuth, tempApiKey);
            HostBuilderSettings settings = HostBuilderSettings.CreateMonitor(urls, metricUrls, metrics, diagnosticPort, startupAuthMode, configurationFilePath);
            IHost host = HostBuilderHelper.CreateHostBuilder(settings).Build();
            IConfiguration configuration = host.Services.GetRequiredService<IConfiguration>();
            using ConfigurationJsonWriter jsonWriter = new ConfigurationJsonWriter(stream);
            jsonWriter.Write(configuration, full: level == ConfigDisplayLevel.Full, skipNotPresent: false, showSources: showSources);
        }
    }
}
