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
        private const string ProductFolderName = "dotnet-monitor";

        // Allows tests to override the shared configuration directory so there
        // is better control and access of what is visible during test.
        private const string SharedConfigDirectoryOverrideEnvironmentVariable
            = "DotnetMonitorTestSettings__SharedConfigDirectoryOverride";

        // Allows tests to override the user configuration directory so there
        // is better control and access of what is visible during test.
        private const string UserConfigDirectoryOverrideEnvironmentVariable
            = "DotnetMonitorTestSettings__UserConfigDirectoryOverride";

        // Allows tests to override the user configuration settings directory so there
        // is better control and access of what is visible during test.
        //private const string UserConfigSettingsDirectoryOverrideEnvironmentVariable
        //    = "DotnetMonitorTestSettings__UserConfigSettingsDirectoryOverride";

        //public static string UserConfigSettingsDirectoryOverrideEnvironmentVariable = null;

        //private const string TestingModeEnvironmentVariable = "DotnetMonitorTestSettings__TestingMode";

        // Location where shared dotnet-monitor configuration is stored.
        // Windows: "%ProgramData%\dotnet-monitor
        // Other: /etc/dotnet-monitor
        private static readonly string SharedConfigDirectoryPath =
            GetEnvironmentOverrideOrValue(
                SharedConfigDirectoryOverrideEnvironmentVariable,
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), ProductFolderName) :
                    Path.Combine("/etc", ProductFolderName));

        private static readonly string SharedSettingsPath = Path.Combine(SharedConfigDirectoryPath, SettingsFileName);

        // Location where user's dotnet-monitor configuration is stored.
        // Windows: "%USERPROFILE%\.dotnet-monitor"
        // Other: "%XDG_CONFIG_HOME%/dotnet-monitor" OR "%HOME%/.config/dotnet-monitor"
        private static readonly string UserConfigDirectoryPath =
            GetEnvironmentOverrideOrValue(
                UserConfigDirectoryOverrideEnvironmentVariable,
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "." + ProductFolderName) :
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ProductFolderName));

        public static string UserSettingsPath = Path.Combine(UserConfigDirectoryPath, SettingsFileName);
        /*GetEnvironmentOverrideOrValue(
            UserConfigSettingsDirectoryOverrideEnvironmentVariable,
            Path.Combine(UserConfigDirectoryPath, SettingsFileName));
        */
        public static ConfigurationTestingMode TestingMode = ConfigurationTestingMode.None;
            /*(ConfigurationTestingMode)Enum.Parse(typeof(ConfigurationTestingMode), GetEnvironmentOverrideOrValue(
                TestingModeEnvironmentVariable,
                ConfigurationTestingMode.None.ToString()));
            */
        public static IHostBuilder CreateHostBuilder(string[] urls, string[] metricUrls, bool metrics, string diagnosticPort, bool noAuth, bool tempApiKey)
        {
            return CreateHostBuilder(urls, metricUrls, metrics, diagnosticPort, CreateAuthConfiguration(noAuth, tempApiKey));
        }

        public static IHostBuilder CreateHostBuilder(string[] urls, string[] metricUrls, bool metrics, string diagnosticPort, AuthConfiguration authenticationOptions)
        {
            return Host.CreateDefaultBuilder()
                .UseContentRoot(AppContext.BaseDirectory) // Use the application root instead of the current directory
                .ConfigureHostConfiguration((IConfigurationBuilder builder) =>
                {
                    if (TestingMode == ConfigurationTestingMode.None)
                    {
                        //Note these are in precedence order.
                        ConfigureEndpointInfoSource(builder, diagnosticPort);
                        ConfigureMetricsEndpoint(builder, metrics, metricUrls);
                        ConfigureGlobalMetrics(builder);
                        builder.ConfigureStorageDefaults();

                        builder.AddCommandLine(new[] { "--urls", ConfigurationHelper.JoinValue(urls) });
                    }
                })
                .ConfigureAppConfiguration((IConfigurationBuilder builder) =>
                {
                    if (TestingMode != ConfigurationTestingMode.None)
                    {
                        while (builder.Sources.Count > 0)
                        {
                            builder.Sources.RemoveAt(0);
                        }
                    }

                    builder.AddJsonFile(UserSettingsPath, optional: true, reloadOnChange: true);
                    builder.AddJsonFile(SharedSettingsPath, optional: true, reloadOnChange: true);

                    if (TestingMode == ConfigurationTestingMode.None)
                    {
                        //HACK Workaround for https://github.com/dotnet/runtime/issues/36091
                        //KeyPerFile provider uses a file system watcher to trigger changes.
                        //The watcher does not follow symlinks inside the watched directory, such as mounted files
                        //in Kubernetes.
                        //We get around this by watching the target folder of the symlink instead.
                        //See https://github.com/kubernetes/kubernetes/master/pkg/volume/util/atomic_writer.go
                        string path = SharedConfigDirectoryPath;
                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && RuntimeInfo.IsInKubernetes)
                        {
                            string symlinkTarget = Path.Combine(SharedConfigDirectoryPath, "..data");
                            if (Directory.Exists(symlinkTarget))
                            {
                                path = symlinkTarget;
                            }
                        }

                        builder.AddKeyPerFile(path, optional: true, reloadOnChange: true);
                        builder.AddEnvironmentVariables(ConfigPrefix);
                    }

                    if (authenticationOptions.KeyAuthenticationMode == KeyAuthenticationMode.TemporaryKey)
                    {
                        ConfigureTempApiHashKey(builder, authenticationOptions);
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
                        //1) ConfigureKestrel settings
                        //2) Command line arguments (--urls)
                        //3) Environment variables (ASPNETCORE_URLS, then DOTNETCORE_URLS)

                        //Our precedence
                        //1) Environment variables (ASPNETCORE_URLS, DotnetMonitor_Metrics__Endpoints)
                        //2) Command line arguments (these have defaults) --urls, --metricUrls
                        //3) ConfigureKestrel is used for fine control of the server, but honors the first two configurations.

                        string hostingUrl = context.Configuration.GetValue<string>(WebHostDefaults.ServerUrlsKey);
                        urls = ConfigurationHelper.SplitValue(hostingUrl);

                        var metricsOptions = new MetricsOptions();
                        context.Configuration.Bind(ConfigurationKeys.Metrics, metricsOptions);

                        string metricHostingUrls = metricsOptions.Endpoints;
                        metricUrls = ConfigurationHelper.SplitValue(metricHostingUrls);

                        //Workaround for lack of default certificate. See https://github.com/dotnet/aspnetcore/issues/28120
                        options.Configure(context.Configuration.GetSection("Kestrel")).Load();

                        //By default, we bind to https for sensitive data (such as dumps and traces) and bind http for
                        //non-sensitive data such as metrics. We may be missing a certificate for https binding. We want to continue with the
                        //http binding in that scenario.
                        listenResults.Listen(
                            options,
                            urls,
                            metricsOptions.Enabled.GetValueOrDefault(MetricsOptionsDefaults.Enabled) ? metricUrls : Array.Empty<string>());
                    })
                    .UseStartup<Startup>();
                });
        }

        public static AuthConfiguration CreateAuthConfiguration(bool noAuth, bool tempApiKey)
        {
            KeyAuthenticationMode authMode = noAuth ? KeyAuthenticationMode.NoAuth : tempApiKey ? KeyAuthenticationMode.TemporaryKey : KeyAuthenticationMode.StoredKey;
            return new AuthConfiguration(authMode);
        }

        private static void ConfigureTempApiHashKey(IConfigurationBuilder builder, AuthConfiguration authenticationOptions)
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
            DiagnosticPortConnectionMode connectionMode = GetConnectionMode(diagnosticPort);
            builder.AddInMemoryCollection(new Dictionary<string, string>
            {
                {ConfigurationPath.Combine(ConfigurationKeys.DiagnosticPort, nameof(DiagnosticPortOptions.ConnectionMode)), connectionMode.ToString()},
                {ConfigurationPath.Combine(ConfigurationKeys.DiagnosticPort, nameof(DiagnosticPortOptions.EndpointName)), diagnosticPort}
            });
        }

        private static DiagnosticPortConnectionMode GetConnectionMode(string diagnosticPort)
        {
            return string.IsNullOrEmpty(diagnosticPort) ? DiagnosticPortConnectionMode.Connect : DiagnosticPortConnectionMode.Listen;
        }

        private static string GetEnvironmentOverrideOrValue(string overrideEnvironmentVariable, string value)
        {
            return Environment.GetEnvironmentVariable(overrideEnvironmentVariable) ?? value;
        }
    }
}
