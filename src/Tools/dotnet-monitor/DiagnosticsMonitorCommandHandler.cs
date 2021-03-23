// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Diagnostics.Monitoring;
using Microsoft.Diagnostics.Monitoring.RestServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class DiagnosticsMonitorCommandHandler
    {
        private const string ConfigPrefix = "DotnetMonitor_";
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

        private static readonly string UserSettingsPath = Path.Combine(UserConfigDirectoryPath, SettingsFileName);

        public async Task<int> Start(CancellationToken token, IConsole console, string[] urls, string[] metricUrls, bool metrics, string diagnosticPort, bool noAuth)
        {
            //CONSIDER The console logger uses the standard AddConsole, and therefore disregards IConsole.
            using IHost host = CreateHostBuilder(console, urls, metricUrls, metrics, diagnosticPort, noAuth).Build();
            try
            {
                await host.RunAsync(token);
            }
            catch (MonitoringException)
            {
                // It is the responsibility of throwers to ensure that the exceptions are logged.
                return -1;
            }
            return 0;
        }

        public static IHostBuilder CreateHostBuilder(IConsole console, string[] urls, string[] metricUrls, bool metrics, string diagnosticPort, bool noAuth)
        {
            return Host.CreateDefaultBuilder()
                .UseContentRoot(AppContext.BaseDirectory) // Use the application root instead of the current directory
                .ConfigureAppConfiguration((IConfigurationBuilder builder) =>
                {
                    //Note these are in precedence order.
                    ConfigureEndpointInfoSource(builder, diagnosticPort);
                    ConfigureMetricsEndpoint(builder, metrics, metricUrls);
                    ConfigureStorageDefaults(builder);

                    builder.AddCommandLine(new[] { "--urls", ConfigurationHelper.JoinValue(urls) });

                    builder.AddJsonFile(UserSettingsPath, optional: true, reloadOnChange: true);
                    builder.AddJsonFile(SharedSettingsPath, optional: true, reloadOnChange: true);

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
                })
                .ConfigureServices((HostBuilderContext context, IServiceCollection services) =>
                {
                    //TODO Many of these service additions should be done through extension methods

                    AuthOptions authenticationOptions = new AuthOptions(noAuth ? KeyAuthenticationMode.NoAuth : KeyAuthenticationMode.StoredKey);
                    services.AddSingleton<IAuthOptions>(authenticationOptions);

                    List<string> authSchemas = null;
                    if (authenticationOptions.EnableKeyAuth)
                    {
                        //Add support for Authentication and Authorization.
                        AuthenticationBuilder authBuilder = services.AddAuthentication(options =>
                        {
                            options.DefaultAuthenticateScheme = AuthConstants.ApiKeySchema;
                            options.DefaultChallengeScheme = AuthConstants.ApiKeySchema;
                        })
                        .AddScheme<ApiKeyAuthenticationHandlerOptions, ApiKeyAuthenticationHandler>(AuthConstants.ApiKeySchema, _ => { });

                        authSchemas = new List<string> { AuthConstants.ApiKeySchema };

                        if (authenticationOptions.EnableNegotiate)
                        {
                            //On Windows add Negotiate package. This will use NTLM to perform Windows Authentication.
                            authBuilder.AddNegotiate();
                            authSchemas.Add(AuthConstants.NegotiateSchema);
                        }
                    }

                    //Apply Authorization Policy for NTLM. Without Authorization, any user with a valid login/password will be authorized. We only
                    //want to authorize the same user that is running dotnet-monitor, at least for now.
                    //Note this policy applies to both Authorization schemas.
                    services.AddAuthorization(authOptions =>
                    {
                        if (authenticationOptions.EnableKeyAuth)
                        {
                            authOptions.AddPolicy(AuthConstants.PolicyName, (builder) =>
                            {
                                builder.AddRequirements(new AuthorizedUserRequirement());
                                builder.RequireAuthenticatedUser();
                                builder.AddAuthenticationSchemes(authSchemas.ToArray());

                            });
                        }
                        else
                        {
                            authOptions.AddPolicy(AuthConstants.PolicyName, (builder) =>
                            {
                                builder.RequireAssertion((_) => true);
                            });
                        }
                    });

                    if (authenticationOptions.EnableKeyAuth)
                    {
                        services.AddSingleton<IAuthorizationHandler, UserAuthorizationHandler>();
                    }

                    services.Configure<DiagnosticPortOptions>(context.Configuration.GetSection(ConfigurationKeys.DiagnosticPort));
                    services.AddSingleton<IEndpointInfoSource, FilteredEndpointInfoSource>();
                    services.AddHostedService<FilteredEndpointInfoSourceHostedService>();
                    services.AddSingleton<IDiagnosticServices, DiagnosticServices>();
                    services.ConfigureEgress(context.Configuration);
                    services.ConfigureMetrics(context.Configuration);
                    services.ConfigureStorage(context.Configuration);
                    services.AddSingleton<ExperimentalToolLogger>();
                })
                .ConfigureLogging(builder =>
                {
                    // Always allow the experimental tool message to be logged
                    ExperimentalToolLogger.AddLogFilter(builder);
                })
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

                        //Workaround for lack of default certificate. See https://github.com/dotnet/aspnetcore/issues/28120
                        options.Configure(context.Configuration.GetSection("Kestrel")).Load();

                        //By default, we bind to https for sensitive data (such as dumps and traces) and bind http for
                        //non-sensitive data such as metrics. We may be missing a certificate for https binding. We want to continue with the
                        //http binding in that scenario.
                        metricUrls = metricsOptions.Enabled.GetValueOrDefault(MetricsOptionsDefaults.Enabled) ?
                            ProcessMetricUrls(metricUrls, metricsOptions) :
                            Array.Empty<string>();

                        listenResults.Listen(options, urls, metricUrls);
                    })
                    .UseStartup<Startup>();
                });
        }

        private static string[] ProcessMetricUrls(string[] metricUrls, MetricsOptions metricsOptions)
        {
            string metricUrlFromConfig = metricsOptions.Endpoints;
            if (!string.IsNullOrEmpty(metricUrlFromConfig))
            {
                metricUrls = ConfigurationHelper.SplitValue(metricUrlFromConfig);
            }

            //If we have custom metrics we want to upgrade the metrics transport channel to https, but
            //also respect the user's configuration to leave it insecure.
            if ((metricsOptions.Providers.Count > 0) &&
                (!metricsOptions.AllowInsecureChannelForCustomMetrics.GetValueOrDefault(false)) &&
                (metricsOptions.Providers.Any(provider =>
                    !Monitoring.EventPipe.MonitoringSourceConfiguration.DefaultMetricProviders.Contains(provider.ProviderName, StringComparer.OrdinalIgnoreCase))))
            {
                for (int i = 0; i < metricUrls.Length; i++)
                {
                    BindingAddress metricUrl = BindingAddress.Parse(metricUrls[i]);

                    //Upgrade http to https by default.
                    if (metricUrl.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
                    {
                        //Based on BindAddress.ToString
                        metricUrls[i] = string.Concat(Uri.UriSchemeHttps.ToLowerInvariant(),
                            Uri.SchemeDelimiter,
                            metricUrl.Host.ToLowerInvariant(),
                            ":",
                            metricUrl.Port.ToString(System.Globalization.CultureInfo.InvariantCulture),
                            metricUrl.PathBase);
                    }
                }
            }

            return metricUrls;
        }

        private static void ConfigureStorageDefaults(IConfigurationBuilder builder)
        {
            builder.AddInMemoryCollection(new Dictionary<string, string>
            {
                {ConfigurationHelper.MakeKey(ConfigurationKeys.Storage, nameof(StorageOptions.DumpTempFolder)), StorageOptionsDefaults.DumpTempFolder }
            });
        }

        private static void ConfigureMetricsEndpoint(IConfigurationBuilder builder, bool enableMetrics, string[] metricEndpoints)
        {
            builder.AddInMemoryCollection(new Dictionary<string, string>
            {
                {ConfigurationHelper.MakeKey(ConfigurationKeys.Metrics, nameof(MetricsOptions.Endpoints)), string.Join(';', metricEndpoints)},
                {ConfigurationHelper.MakeKey(ConfigurationKeys.Metrics, nameof(MetricsOptions.Enabled)), enableMetrics.ToString()},
                {ConfigurationHelper.MakeKey(ConfigurationKeys.Metrics, nameof(MetricsOptions.UpdateIntervalSeconds)), MetricsOptionsDefaults.UpdateIntervalSeconds.ToString()},
                {ConfigurationHelper.MakeKey(ConfigurationKeys.Metrics, nameof(MetricsOptions.MetricCount)), MetricsOptionsDefaults.MetricCount.ToString()},
                {ConfigurationHelper.MakeKey(ConfigurationKeys.Metrics, nameof(MetricsOptions.IncludeDefaultProviders)), MetricsOptionsDefaults.IncludeDefaultProviders.ToString()},
                {ConfigurationHelper.MakeKey(ConfigurationKeys.Metrics, nameof(MetricsOptions.AllowInsecureChannelForCustomMetrics)), MetricsOptionsDefaults.AllowInsecureChannelForCustomMetrics.ToString()}
            });
        }

        private static void ConfigureEndpointInfoSource(IConfigurationBuilder builder, string diagnosticPort)
        {
            DiagnosticPortConnectionMode connectionMode = string.IsNullOrEmpty(diagnosticPort) ? DiagnosticPortConnectionMode.Connect : DiagnosticPortConnectionMode.Listen;
            builder.AddInMemoryCollection(new Dictionary<string, string>
            {
                {ConfigurationHelper.MakeKey(ConfigurationKeys.DiagnosticPort, nameof(DiagnosticPortOptions.ConnectionMode)), connectionMode.ToString()},
                {ConfigurationHelper.MakeKey(ConfigurationKeys.DiagnosticPort, nameof(DiagnosticPortOptions.EndpointName)), diagnosticPort}
            });
        }

        private static string GetEnvironmentOverrideOrValue(string overrideEnvironmentVariable, string value)
        {
            return Environment.GetEnvironmentVariable(overrideEnvironmentVariable) ?? value;
        }
    }
}
