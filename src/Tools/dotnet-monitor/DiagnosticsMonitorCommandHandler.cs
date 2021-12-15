// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Diagnostics.Monitoring;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Actions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using IEndpointInfoSource = Microsoft.Diagnostics.Monitoring.WebApi.IEndpointInfoSource;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class DiagnosticsMonitorCommandHandler
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

        // Location where shared dotnet-monitor configuration is stored.
        // Windows: "%ProgramData%\dotnet-monitor
        // Other: /etc/dotnet-monitor
        private static readonly string SharedConfigDirectoryPath =
            GetEnvironmentOverrideOrValue(
                SharedConfigDirectoryOverrideEnvironmentVariable,
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), ProductFolderName):
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

        public async Task<int> Start(CancellationToken token, IConsole console, string[] urls, string[] metricUrls, bool metrics, string diagnosticPort, bool noAuth, bool tempApiKey, bool noHttpEgress)
        {
            //CONSIDER The console logger uses the standard AddConsole, and therefore disregards IConsole.
            try
            {
                IHost host = CreateHostBuilder(console, urls, metricUrls, metrics, diagnosticPort, noAuth, tempApiKey, noHttpEgress, configOnly: false).Build();
                try
                {
                    await host.StartAsync(token);

                    await host.WaitForShutdownAsync(token);
                }
                catch (MonitoringException)
                {
                    // It is the responsibility of throwers to ensure that the exceptions are logged.
                    return -1;
                }
                catch (OptionsValidationException ex)
                {
                    host.Services.GetRequiredService<ILoggerFactory>()
                        .CreateLogger(typeof(DiagnosticsMonitorCommandHandler))
                        .OptionsValidationFailure(ex);
                    return -1;
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    // The host will throw a OperationCanceledException if it cannot shut down the
                    // hosted services gracefully within the shut down timeout period. Handle the
                    // exception and let the tool exit gracefully.
                    return 0;
                }
                finally
                {
                    if (host is IAsyncDisposable asyncDisposable)
                    {
                        await asyncDisposable.DisposeAsync();
                    }
                    else
                    {
                        host.Dispose();
                    }
                }
            }
            catch (FormatException ex)
            {
                Console.Error.WriteLine(ex.Message);
                if (ex.InnerException != null)
                {
                    Console.Error.WriteLine(ex.InnerException.Message);
                }

                return -1;
            }

            return 0;
        }

        public Task<int> ShowConfig(CancellationToken token, IConsole console, string[] urls, string[] metricUrls, bool metrics, string diagnosticPort, bool noAuth, bool tempApiKey, bool noHttpEgress, ConfigDisplayLevel level)
        {
            IHost host = CreateHostBuilder(console, urls, metricUrls, metrics, diagnosticPort, noAuth, tempApiKey, noHttpEgress, configOnly: true).Build();
            IConfiguration configuration = host.Services.GetRequiredService<IConfiguration>();
            using ConfigurationJsonWriter writer = new ConfigurationJsonWriter(Console.OpenStandardOutput());
            writer.Write(configuration, full: level == ConfigDisplayLevel.Full, skipNotPresent: false);
            
            return Task.FromResult(0);
        }

        public static IHostBuilder CreateHostBuilder(IConsole console, string[] urls, string[] metricUrls, bool metrics, string diagnosticPort, bool noAuth, bool tempApiKey, bool noHttpEgress, bool configOnly)
        {
            IHostBuilder hostBuilder = Host.CreateDefaultBuilder();

            KeyAuthenticationMode authMode = noAuth ? KeyAuthenticationMode.NoAuth : tempApiKey ? KeyAuthenticationMode.TemporaryKey : KeyAuthenticationMode.StoredKey;
            AuthConfiguration authenticationOptions = new AuthConfiguration(authMode);

            EgressOutputConfiguration egressConfiguration = new EgressOutputConfiguration(httpEgressEnabled: !noHttpEgress);

            hostBuilder.UseContentRoot(AppContext.BaseDirectory) // Use the application root instead of the current directory
                .ConfigureHostConfiguration((IConfigurationBuilder builder) =>
                {
                    //Note these are in precedence order.
                    ConfigureEndpointInfoSource(builder, diagnosticPort);
                    ConfigureMetricsEndpoint(builder, metrics, metricUrls);
                    ConfigureGlobalMetrics(builder);
                    builder.ConfigureStorageDefaults();

                    builder.AddCommandLine(new[] { "--urls", ConfigurationHelper.JoinValue(urls) });
                })
                .ConfigureAppConfiguration((IConfigurationBuilder builder) =>
                {
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

                    if (authenticationOptions.KeyAuthenticationMode == KeyAuthenticationMode.TemporaryKey)
                    {
                        ConfigureTempApiHashKey(builder, authenticationOptions);
                    }
                });

            if (!configOnly)
            {
                hostBuilder.ConfigureServices((HostBuilderContext context, IServiceCollection services) =>
                {
                    //TODO Many of these service additions should be done through extension methods

                    services.AddSingleton<IAuthConfiguration>(authenticationOptions);

                    services.AddSingleton<IEgressOutputConfiguration>(egressConfiguration);

                    // Although this is only observing API key authentication changes, it does handle
                    // the case when API key authentication is not enabled. This class could evolve
                    // to observe other options in the future, at which point it might be good to
                    // refactor the options observers for each into separate implementations and are
                    // orchestrated by this single service.
                    services.AddSingleton<MonitorApiKeyConfigurationObserver>();

                    List<string> authSchemas = null;
                    if (authenticationOptions.EnableKeyAuth)
                    {
                        AuthenticationBuilder authBuilder = services.ConfigureMonitorApiKeyAuthentication(context.Configuration);
                                                
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
                    services.AddSingleton<IValidateOptions<DiagnosticPortOptions>, DiagnosticPortValidateOptions>();
                    services.AddSingleton<OperationTrackerService>();

                    services.ConfigureGlobalCounter(context.Configuration);

                    services.AddSingleton<IEndpointInfoSource, FilteredEndpointInfoSource>();
                    services.AddSingleton<ServerEndpointInfoSource>();
                    services.AddHostedServiceForwarder<ServerEndpointInfoSource>();
                    services.AddSingleton<IDiagnosticServices, DiagnosticServices>();
                    services.AddSingleton<IDumpService, DumpService>();
                    services.AddSingleton<IEndpointInfoSourceCallbacks, OperationTrackerServiceEndpointInfoSourceCallback>();
                    services.AddSingleton<RequestLimitTracker>();
                    services.ConfigureOperationStore();
                    services.ConfigureEgress();
                    services.ConfigureMetrics(context.Configuration);
                    services.ConfigureStorage(context.Configuration);
                    services.ConfigureDefaultProcess(context.Configuration);
                    services.ConfigureCollectionRules();
                });
            }

            //Note this is necessary for config only because Kestrel configuration
            //is not added until WebHostDefaults are added.
            hostBuilder.ConfigureWebHostDefaults(webBuilder =>
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

            return hostBuilder;
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
