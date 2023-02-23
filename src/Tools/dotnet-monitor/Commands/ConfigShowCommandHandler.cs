// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.Auth;
using Microsoft.Diagnostics.Tools.Monitor.Auth.ApiKey.Temporary;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Diagnostics.Tools.Monitor.Commands
{
    internal sealed class ConfigShowCommandHandler
    {
        // Although the "noHttpEgress" parameter is unused, it keeps the entire command parameter set a superset
        // of the "collect" command so that users can take the same arguments from "collect" and use it on "config show"
        // to get the same configuration without telling them to drop specific command line arguments.
        public static void Invoke(string[] urls, string[] metricUrls, bool metrics, string diagnosticPort, bool noAuth, bool tempApiKey, bool noHttpEgress, FileInfo configurationFilePath, ConfigDisplayLevel level, bool showSources)
        {
            Stream stream = Console.OpenStandardOutput();

            using StreamWriter writer = new(stream, EncodingCache.UTF8NoBOMNoThrow, 1024, leaveOpen: true);
            writer.WriteLine(ExperienceSurvey.ExperienceSurveyMessage);
            writer.WriteLine();
            writer.Flush();

            Write(stream, urls, metricUrls, metrics, diagnosticPort, noAuth, tempApiKey, configurationFilePath, level, showSources);
        }

        public static void Write(Stream stream, string[] urls, string[] metricUrls, bool metrics, string diagnosticPort, bool noAuth, bool tempApiKey, FileInfo configurationFilePath, ConfigDisplayLevel level, bool showSources)
        {
            StartupAuthenticationMode startupAuthMode = HostBuilderHelper.GetStartupAuthenticationMode(noAuth, tempApiKey);
            HostBuilderSettings settings = HostBuilderSettings.CreateMonitor(urls, metricUrls, metrics, diagnosticPort, startupAuthMode, configurationFilePath);
            IHost host = HostBuilderHelper.CreateHostBuilder(settings)
                .ConfigureAppConfiguration((HostBuilderContext context, IConfigurationBuilder builder) =>
                {
                    // HACK: generate a random jwt key and add it to the command line options.
                    // Since we never fully startup it doesn't have to actually match what the auth configurator is using.
                    //
                    // Do this to avoid the breaking change described in https://github.com/dotnet/dotnet-monitor/pull/3665 in already shipping major versions.
                    if (startupAuthMode == StartupAuthenticationMode.TemporaryKey)
                    {
                        List<string> arguments = new();

                        GeneratedJwtKey generatedJwtKey = GeneratedJwtKey.Create();
                        arguments.Add(HostBuilderHelper.FormatCmdLineArgument(
                            ConfigurationPath.Combine(ConfigurationKeys.Authentication, ConfigurationKeys.MonitorApiKey, nameof(MonitorApiKeyOptions.Subject)),
                            generatedJwtKey.Subject));

                        arguments.Add(HostBuilderHelper.FormatCmdLineArgument(
                            ConfigurationPath.Combine(ConfigurationKeys.Authentication, ConfigurationKeys.MonitorApiKey, nameof(MonitorApiKeyOptions.PublicKey)),
                            generatedJwtKey.PublicKey));

                        builder.AddCommandLine(arguments.ToArray());
                    }
                })
                .Build();
            IConfiguration configuration = host.Services.GetRequiredService<IConfiguration>();
            using ConfigurationJsonWriter jsonWriter = new ConfigurationJsonWriter(stream);
            jsonWriter.Write(configuration, full: level == ConfigDisplayLevel.Full, skipNotPresent: false, showSources: showSources);
        }
    }
}
