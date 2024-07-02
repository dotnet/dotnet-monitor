// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.Auth;
using Microsoft.Diagnostics.Tools.Monitor.Auth.ApiKey;
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
        private const string SettingsFileName = "settings.json";

        public static IHostBuilder CreateHostBuilder(HostBuilderSettings settings)
        {
            string? aspnetUrls = string.Empty;
            ServerUrlsBlockingConfigurationManager manager = new();
            manager.IsBlocking = true;

            return new HostBuilder()
                .ConfigureHostConfiguration((IConfigurationBuilder builder) =>
                {
                    // Configure default values
                    ConfigureGlobalMetricsDefaults(builder);
                    ConfigureMetricsDefaults(builder);

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
                    context.Properties[typeof(ServerUrlsBlockingConfigurationManager)] = manager;

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
                    string? path = settings.SharedConfigDirectory;
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
                    builder.AddEnvironmentVariables(ToolIdentifiers.StandardPrefix);

                    // User-specified configuration file path is considered highest precedence, but does NOT override other configuration sources
                    FileInfo? userFilePath = settings.UserProvidedConfigFilePath;

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

                    if (settings.AuthenticationMode == StartupAuthenticationMode.TemporaryKey)
                    {
                        GeneratedJwtKey jwtKey = GeneratedJwtKey.Create(AuthConstants.ApiKeyJwtDefaultExpiration);
                        context.Properties.Add(typeof(GeneratedJwtKey), jwtKey);

                        // These are configured via the command line configuration source so that
                        // the "show config" command will report these are from the command line
                        // rather than an in-memory collection.
                        List<string> arguments = new();
                        AddTempApiKeyArguments(arguments, jwtKey);

                        builder.AddCommandLine(arguments.ToArray());
                    }
                })
                //Note this is necessary for config only because Kestrel configuration
                //is not added until WebHostDefaults are added.
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    TestAssemblies.AddHostingStartup(webBuilder);

                    // ASP.NET will initially create a configuration that primarily contains
                    // the ASPNETCORE_* environment variables. This IWebHostBuilder configuration callback
                    // is invoked before any of the usual configuration phases (host, app, service, container)
                    // are executed. Thus, there is opportunity here to get the Urls option to store it and
                    // clear it so that the initial WebHostOptions does not pick it up during host configuration.
                    aspnetUrls = webBuilder.GetSetting(WebHostDefaults.ServerUrlsKey);
                    webBuilder.UseSetting(WebHostDefaults.ServerUrlsKey, string.Empty);

                    AddressListenResults listenResults = new AddressListenResults();
                    webBuilder.ConfigureServices(services =>
                    {
                        services.AddSingleton(listenResults);
                        services.AddSingleton<IStartupFilter, AddressListenResultsStartupFilter>();
                        services.AddHostedService<StartupLoggingHostedService>();
                    })
                    .ConfigureKestrel((context, options) =>
                    {
                        //Note our priorities for hosting urls don't match the default behavior.
                        //Default Kestrel behavior priority
                        //1) ConfigureKestrel settings
                        //2) Command line arguments (--urls)
                        //3) Environment variables (ASPNETCORE_URLS, then DOTNETCORE_URLS)

                        //Our precedence
                        //1) Command line arguments (these have defaults) --urls, --metricUrls
                        //2) Environment variables (ASPNETCORE_URLS, DotnetMonitor_Metrics__Endpoints)
                        //3) ConfigureKestrel is used for fine control of the server, but honors the first two configurations.

                        // Unblock reading of the Urls option from configuration, read it, and block it again so that Kestrel
                        // is unable to read this option when it starts.
#nullable disable
                        manager.IsBlocking = false;
                        string[] urls = ConfigurationHelper.SplitValue(context.Configuration[WebHostDefaults.ServerUrlsKey]);
                        manager.IsBlocking = true;

                        var metricsOptions = new MetricsOptions();
                        context.Configuration.Bind(ConfigurationKeys.Metrics, metricsOptions);

                        string metricHostingUrls = metricsOptions.Endpoints;
                        string[] metricUrls = ConfigurationHelper.SplitValue(metricHostingUrls);
#nullable restore
                        //Workaround for lack of default certificate. See https://github.com/dotnet/aspnetcore/issues/28120
                        options.Configure(context.Configuration.GetSection("Kestrel")).Load();

                        //By default, we bind to https for sensitive data (such as dumps and traces) and bind http for
                        //non-sensitive data such as metrics. We may be missing a certificate for https binding. We want to continue with the
                        //http binding in that scenario.
                        listenResults.Listen(
                            options,
                            urls,
                            metricsOptions.GetEnabled() ? metricUrls : Array.Empty<string>(),
                            settings.AuthenticationMode != StartupAuthenticationMode.NoAuth);
                    })
                    .UseStartup<Startup>();
                })
                .ConfigureHostConfiguration((IConfigurationBuilder builder) =>
                {
                    // Restore the Urls option to the configuration provider that originally provided the value
                    // before it was cleared during the IWebHostBuilder configuration callback.
                    if (!string.IsNullOrEmpty(aspnetUrls) &&
                        builder.TryGetProvider(WebHostDefaults.ServerUrlsKey, out IConfigurationProvider? provider))
                    {
                        provider.Set(WebHostDefaults.ServerUrlsKey, aspnetUrls);
                    }

                    // The DOTNET_* and ASPNETCORE_* environment variables were added as part of the host configuration
                    // phase. Before the phase is completed, add a configuration source that will conditionally block
                    // reading of the Urls options so that they are not picked up by future Kestrel configuration callbacks.
                    builder.Add(new ServerUrlsBlockingConfigurationSource(manager));
                })
                .ConfigureAppConfiguration((IConfigurationBuilder builder) =>
                {
                    // The settings.json, key-per-file, and DOTNETMONITOR_* environment variables were added as part
                    // of the app configuration phase. Before the phase is completed, add a configuration source that will
                    // conditionally block reading of the Urls options so that they are not picked up by future Kestrel
                    // configuration callbacks.
                    builder.Add(new ServerUrlsBlockingConfigurationSource(manager));
                })
                .ConfigureContainer((HostBuilderContext context, IServiceCollection services) =>
                {
                    // Container configuration is the last phase of building the host before the service provider is constructed.
                    // At this point, all configuration callbacks have been executed. Lift the block on the Urls option so that
                    // the option may be read from configuration by default.
                    manager.IsBlocking = false;
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

        public static StartupAuthenticationMode GetStartupAuthenticationMode(bool noAuth, bool tempApiKey)
        {
            if (noAuth)
            {
                return StartupAuthenticationMode.NoAuth;
            }

            if (tempApiKey)
            {
                return StartupAuthenticationMode.TemporaryKey;
            }

            // The authentication mode wasn't configured by startup arguments.
            // Defer determining which auth mode to use until we can inspect the provided configuration.
            return StartupAuthenticationMode.Deferred;
        }

        private static void ConfigureMetricsDefaults(IConfigurationBuilder builder)
        {
            builder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                {ConfigurationPath.Combine(ConfigurationKeys.Metrics, nameof(MetricsOptions.MetricCount)), MetricsOptionsDefaults.MetricCount.ToString()},
                {ConfigurationPath.Combine(ConfigurationKeys.Metrics, nameof(MetricsOptions.IncludeDefaultProviders)), MetricsOptionsDefaults.IncludeDefaultProviders.ToString()}
            });
        }

        private static void ConfigureGlobalMetricsDefaults(IConfigurationBuilder builder)
        {
            builder.AddInMemoryCollection(new Dictionary<string, string?>
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

        private static void AddTempApiKeyArguments(List<string> arguments, GeneratedJwtKey jwtKey)
        {
            arguments.Add(FormatCmdLineArgument(
                ConfigurationPath.Combine(ConfigurationKeys.Authentication, ConfigurationKeys.MonitorApiKey, nameof(MonitorApiKeyOptions.Subject)),
                jwtKey.Subject));

            arguments.Add(FormatCmdLineArgument(
                ConfigurationPath.Combine(ConfigurationKeys.Authentication, ConfigurationKeys.MonitorApiKey, nameof(MonitorApiKeyOptions.PublicKey)),
                jwtKey.PublicKey));
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
