// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal static class HostBuilderHelper
    {
        public const string ConfigPrefix = "DotnetMonitor_";
        private const string SettingsFileName = "settings.json";

        public static IHostBuilder CreateHostBuilder(HostBuilderSettings settings)
        {
            // The configuration is built in a precedence order such that sources that are configured
            // later will have a higher precedence. Typically, an application would call Host.CreateDefaultBuilder
            // and pass command line arguments to this call. This would setup DOTNET_* environment variables at a
            // lower precedence compared to the command line arguments. For dotnet-monitor, the desired order of
            // precedence is (where 1 is the highest precedence):
            //
            // 1) DOTNETMONITOR_* environment variables
            // 2) User and shared settings and key-per-file files
            // 3) appsettings.*.json files
            // 4) DOTNET_* and ASPNETCORE_* environment variables
            // 5) Command Line Arguments and defaults
            //
            // In order to achieve this, create an empty host builder, add the command line arguments and defaults
            // as host configuration, then configure the defaults (which includes DOTNET_* environment variables
            // and appsettings.*.json files), and finally configure the remaining dotnet-monitor specific sources.
            return new HostBuilder()
                .ConfigureHostConfiguration((IConfigurationBuilder builder) =>
                {
                    // Configure default values
                    ConfigureGlobalMetricsDefaults(builder);
                    ConfigureMetricsDefaults(builder);
                    builder.ConfigureStorageDefaults();

                    // These are configured via the command line configuration source so that
                    // the "show config" command will report these are from the command line
                    // rather than an in-memory collection.
                    List<string> arguments = new();
                    AddDiagnosticPortArguments(arguments, settings);
                    AddMetricsArguments(arguments, settings);
                    AddUrlsArguments(arguments, settings);

                    builder.AddCommandLine(arguments.ToArray());
                })
                .ConfigureDefaults(args: null)
                .UseContentRoot(settings.ContentRootDirectory)
                .ConfigureAppConfiguration((HostBuilderContext context, IConfigurationBuilder builder) =>
                {
                    HostBuilderResults hostBuilderResults = new HostBuilderResults();
                    context.Properties.Add(HostBuilderResults.ResultKey, hostBuilderResults);

                    string userSettingsPath = Path.Combine(settings.UserConfigDirectory, SettingsFileName);
                    AddJsonFileHelper(builder, hostBuilderResults, userSettingsPath);

                    string sharedSettingsPath = Path.Combine(settings.SharedConfigDirectory, SettingsFileName);
                    AddJsonFileHelper(builder, hostBuilderResults, sharedSettingsPath);

                    //HACK Workaround for https://github.com/dotnet/runtime/issues/36091
                    //KeyPerFile provider uses a file system watcher to trigger changes.
                    //The watcher does not follow symlinks inside the watched directory, such as mounted files
                    //in Kubernetes.
                    //We get around this by watching the target folder of the symlink instead.
                    //See https://github.com/kubernetes/kubernetes/master/pkg/volume/util/atomic_writer.go
                    string path = settings.SharedConfigDirectory;
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && RuntimeInfo.IsInKubernetes)
                    {
                        string symlinkTarget = Path.Combine(settings.SharedConfigDirectory, "..data");
                        if (Directory.Exists(symlinkTarget))
                        {
                            path = symlinkTarget;
                        }
                    }

                    // If a file at this path does not have read permissions, the application will fail to launch.
                    builder.AddKeyPerFile(path, optional: true, reloadOnChange: true);
                    builder.AddEnvironmentVariables(ConfigPrefix);

                    if (settings.Authentication.KeyAuthenticationMode == KeyAuthenticationMode.TemporaryKey)
                    {
                        // These are configured via the command line configuration source so that
                        // the "show config" command will report these are from the command line
                        // rather than an in-memory collection.
                        List<string> arguments = new();
                        AddTempApiKeyArguments(arguments, settings);

                        builder.AddCommandLine(arguments.ToArray());
                    }

                    // User-specified configuration file path is considered highest precedence, but does NOT override other configuration sources
                    FileInfo userFilePath = settings.UserProvidedConfigFilePath;

                    if (null != userFilePath)
                    {
                        if (!userFilePath.Exists)
                        {
                            hostBuilderResults.Warnings.Add(string.Format(Strings.Message_ConfigurationFileDoesNotExist, userFilePath.FullName));
                        }
                        else if (!".json".Equals(userFilePath.Extension, StringComparison.OrdinalIgnoreCase))
                        {
                            hostBuilderResults.Warnings.Add(string.Format(Strings.Message_ConfigurationFileNotJson, userFilePath.FullName));
                        }
                        else
                        {
                            AddJsonFileHelper(builder, hostBuilderResults, userFilePath.FullName);
                        }
                    }
                })
                //Note this is necessary for config only because Kestrel configuration
                //is not added until WebHostDefaults are added.
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    AddressListenResults listenResults = new AddressListenResults();
                    webBuilder.ConfigureServices(services =>
                    {
                        services.AddSingleton(listenResults);
                    })
                    .ConfigureKestrel((context, options) =>
                    {
                        //Note our priorities for hosting urls don't match the default behavior.
                        //Default Kestrel behavior priority
                        // (1 is the higher precedence)
                        //1) ConfigureKestrel settings
                        //2) Command line arguments (--urls)
                        //3) Environment variables (ASPNETCORE_URLS, then DOTNETCORE_URLS)

                        //Our precedence
                        //1) Environment variables (ASPNETCORE_URLS, DotnetMonitor_Metrics__Endpoints)
                        //2) Command line arguments (these have defaults) --urls, --metricUrls
                        //3) ConfigureKestrel is used for fine control of the server, but honors the first two configurations.

                        string hostingUrl = context.Configuration.GetValue<string>(WebHostDefaults.ServerUrlsKey);
                        string[] urls = ConfigurationHelper.SplitValue(hostingUrl);

                        var metricsOptions = new MetricsOptions();
                        context.Configuration.Bind(ConfigurationKeys.Metrics, metricsOptions);

                        string metricHostingUrls = metricsOptions.Endpoints;
                        string[] metricUrls = ConfigurationHelper.SplitValue(metricHostingUrls);

                        //Workaround for lack of default certificate. See https://github.com/dotnet/aspnetcore/issues/28120
                        options.Configure(context.Configuration.GetSection("Kestrel")).Load();

                        //By default, we bind to https for sensitive data (such as dumps and traces) and bind http for
                        //non-sensitive data such as metrics. We may be missing a certificate for https binding. We want to continue with the
                        //http binding in that scenario.
                        listenResults.Listen(
                            options,
                            urls,
                            metricsOptions.Enabled.GetValueOrDefault(MetricsOptionsDefaults.Enabled) ? metricUrls : Array.Empty<string>(),
                            settings.Authentication.KeyAuthenticationMode != KeyAuthenticationMode.NoAuth);

                        // Since the endpoints have already been defined on the KestrelServerOptions, clear out the urls
                        // so that the address binder does not log a warning about overriding urls as specified by configuration.
                        context.Configuration[WebHostDefaults.ServerUrlsKey] = string.Empty;
                    })
                    .UseStartup<Startup>();
                });
        }

        private static void AddJsonFileHelper(IConfigurationBuilder builder, HostBuilderResults hostBuilderResults, string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.OpenRead(filePath).Dispose(); // If this succeeds, we have read permissions
                    builder.AddJsonFile(filePath, optional: true, reloadOnChange: true);
                }
            }
            catch (Exception ex)
            {
                hostBuilderResults.Warnings.Add(ex.Message);
            }
        }

        public static AuthConfiguration CreateAuthConfiguration(bool noAuth, bool tempApiKey)
        {
            KeyAuthenticationMode authMode = noAuth ? KeyAuthenticationMode.NoAuth : tempApiKey ? KeyAuthenticationMode.TemporaryKey : KeyAuthenticationMode.StoredKey;
            return new AuthConfiguration(authMode);
        }

        private static void ConfigureMetricsDefaults(IConfigurationBuilder builder)
        {
            builder.AddInMemoryCollection(new Dictionary<string, string>
            {
                {ConfigurationPath.Combine(ConfigurationKeys.Metrics, nameof(MetricsOptions.MetricCount)), MetricsOptionsDefaults.MetricCount.ToString()},
                {ConfigurationPath.Combine(ConfigurationKeys.Metrics, nameof(MetricsOptions.IncludeDefaultProviders)), MetricsOptionsDefaults.IncludeDefaultProviders.ToString()}
            });
        }

        private static void ConfigureGlobalMetricsDefaults(IConfigurationBuilder builder)
        {
            builder.AddInMemoryCollection(new Dictionary<string, string>
            {
                {ConfigurationPath.Combine(ConfigurationKeys.GlobalCounter, nameof(GlobalCounterOptions.IntervalSeconds)), GlobalCounterOptionsDefaults.IntervalSeconds.ToString() }
            });
        }

        private static void AddDiagnosticPortArguments(List<string> arguments, HostBuilderSettings settings)
        {
            if (!string.IsNullOrEmpty(settings.DiagnosticPort))
            {
                arguments.Add(FormatCmdLineArgument(
                    ConfigurationPath.Combine(ConfigurationKeys.DiagnosticPort, nameof(DiagnosticPortOptions.ConnectionMode)),
                    DiagnosticPortConnectionMode.Listen.ToString()));

                arguments.Add(FormatCmdLineArgument(
                    ConfigurationPath.Combine(ConfigurationKeys.DiagnosticPort, nameof(DiagnosticPortOptions.EndpointName)),
                    settings.DiagnosticPort));
            }
        }

        private static void AddMetricsArguments(List<string> arguments, HostBuilderSettings settings)
        {
            arguments.Add(FormatCmdLineArgument(
                ConfigurationPath.Combine(ConfigurationKeys.Metrics, nameof(MetricsOptions.Endpoints)),
                ConfigurationHelper.JoinValue(settings.MetricsUrls ?? Array.Empty<string>())));

            arguments.Add(FormatCmdLineArgument(
                ConfigurationPath.Combine(ConfigurationKeys.Metrics, nameof(MetricsOptions.Enabled)),
                settings.EnableMetrics.ToString()));
        }

        private static void AddTempApiKeyArguments(List<string> arguments, HostBuilderSettings settings)
        {
            if (settings.Authentication.TemporaryJwtKey != null)
            {
                arguments.Add(FormatCmdLineArgument(
                    ConfigurationPath.Combine(ConfigurationKeys.Authentication, ConfigurationKeys.MonitorApiKey, nameof(MonitorApiKeyOptions.Subject)),
                    settings.Authentication.TemporaryJwtKey.Subject));

                arguments.Add(FormatCmdLineArgument(
                    ConfigurationPath.Combine(ConfigurationKeys.Authentication, ConfigurationKeys.MonitorApiKey, nameof(MonitorApiKeyOptions.PublicKey)),
                    settings.Authentication.TemporaryJwtKey.PublicKey));
            }
        }

        private static void AddUrlsArguments(List<string> arguments, HostBuilderSettings settings)
        {
            arguments.Add(FormatCmdLineArgument(
                WebHostDefaults.ServerUrlsKey,
                ConfigurationHelper.JoinValue(settings.Urls ?? Array.Empty<string>())));
        }

        private static string FormatCmdLineArgument(string key, string value)
        {
            return FormattableString.Invariant($"{key}={value}");
        }
    }
}
