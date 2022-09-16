// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.Profiler;
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
            return new HostBuilder()
                .ConfigureHostConfiguration((IConfigurationBuilder builder) =>
                {
                    //Note these are in precedence order.
                    ConfigureEndpointInfoSource(builder, settings.DiagnosticPort);
                    ConfigureMetricsEndpoint(builder, settings.EnableMetrics, settings.MetricsUrls ?? Array.Empty<string>());
                    ConfigureGlobalMetrics(builder);

                    builder.AddCommandLine(new[] { "--urls", ConfigurationHelper.JoinValue(settings.Urls ?? Array.Empty<string>()) });
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
                    builder.AddEnvironmentVariables(ToolIdentifiers.StandardPrefix);

                    if (settings.Authentication.KeyAuthenticationMode == KeyAuthenticationMode.TemporaryKey)
                    {
                        ConfigureTempApiHashKey(builder, settings.Authentication);
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
                    TestAssemblies.AddHostingStartup(webBuilder);

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

        private static void ConfigureTempApiHashKey(IConfigurationBuilder builder, IAuthConfiguration authenticationOptions)
        {
            if (authenticationOptions.TemporaryJwtKey != null)
            {
                builder.AddInMemoryCollection(new Dictionary<string, string>
                {
                    { ConfigurationPath.Combine(ConfigurationKeys.Authentication, ConfigurationKeys.MonitorApiKey, nameof(MonitorApiKeyOptions.Subject)), authenticationOptions.TemporaryJwtKey.Subject },
                    { ConfigurationPath.Combine(ConfigurationKeys.Authentication, ConfigurationKeys.MonitorApiKey, nameof(MonitorApiKeyOptions.PublicKey)), authenticationOptions.TemporaryJwtKey.PublicKey },
                });
            }
        }

        private static void ConfigureMetricsEndpoint(IConfigurationBuilder builder, bool enableMetrics, string[] metricEndpoints)
        {
            builder.AddInMemoryCollection(new Dictionary<string, string>
            {
                {ConfigurationPath.Combine(ConfigurationKeys.Metrics, nameof(MetricsOptions.Endpoints)), string.Join(';', metricEndpoints)},
                {ConfigurationPath.Combine(ConfigurationKeys.Metrics, nameof(MetricsOptions.Enabled)), enableMetrics.ToString()},
                {ConfigurationPath.Combine(ConfigurationKeys.Metrics, nameof(MetricsOptions.MetricCount)), MetricsOptionsDefaults.MetricCount.ToString()},
                {ConfigurationPath.Combine(ConfigurationKeys.Metrics, nameof(MetricsOptions.IncludeDefaultProviders)), MetricsOptionsDefaults.IncludeDefaultProviders.ToString()}
            });
        }

        private static void ConfigureGlobalMetrics(IConfigurationBuilder builder)
        {
            builder.AddInMemoryCollection(new Dictionary<string, string>
            {
                {ConfigurationPath.Combine(ConfigurationKeys.GlobalCounter, nameof(GlobalCounterOptions.IntervalSeconds)), GlobalCounterOptionsDefaults.IntervalSeconds.ToString() }
            });
        }

        private static void ConfigureEndpointInfoSource(IConfigurationBuilder builder, string diagnosticPort)
        {
            if (!string.IsNullOrEmpty(diagnosticPort))
            {
                builder.AddInMemoryCollection(new Dictionary<string, string>
                {
                    {ConfigurationPath.Combine(ConfigurationKeys.DiagnosticPort, nameof(DiagnosticPortOptions.ConnectionMode)), DiagnosticPortConnectionMode.Listen.ToString()},
                    {ConfigurationPath.Combine(ConfigurationKeys.DiagnosticPort, nameof(DiagnosticPortOptions.EndpointName)), diagnosticPort}
                });
            }
        }
    }
}
