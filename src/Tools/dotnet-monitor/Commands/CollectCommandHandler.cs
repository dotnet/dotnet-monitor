// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Diagnostics.Monitoring;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.Auth;
using Microsoft.Diagnostics.Tools.Monitor.Swagger;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.Commands
{
    internal static class CollectCommandHandler
    {
        public static async Task<int> Invoke(CancellationToken token, string[] urls, string[] metricUrls, bool metrics, string diagnosticPort, bool noAuth, bool tempApiKey, bool noHttpEgress, FileInfo configurationFilePath)
        {
            try
            {
                StartupAuthenticationMode authMode = HostBuilderHelper.GetStartupAuthenticationMode(noAuth, tempApiKey);
                HostBuilderSettings settings = HostBuilderSettings.CreateMonitor(urls, metricUrls, metrics, diagnosticPort, authMode, configurationFilePath);
                // Kestrel is configured below, thus simulating the ASP.NET Core configuration is unnecessary.
                settings.SimulateAspNetConfiguration = false;

                IHost host = HostBuilderHelper.CreateHostBuilder(settings)
                    .Configure(authMode, noHttpEgress)
                    .Build();

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
                        .CreateLogger(typeof(CollectCommandHandler))
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
                    await DisposableHelper.DisposeAsync(host);
                }
            }
            catch (Exception ex) when (ex is FormatException || ex is DeferredAuthenticationValidationException)
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

        private static IHostBuilder Configure(this IHostBuilder builder, StartupAuthenticationMode startupAuthMode, bool noHttpEgress)
        {
            string aspnetUrls = string.Empty;
            return builder
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

                        string[] urls = ConfigurationHelper.SplitValue(context.Configuration[WebHostDefaults.ServerUrlsKey]);
                        context.Configuration[WebHostDefaults.ServerUrlsKey] = string.Empty;

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
                            metricsOptions.GetEnabled() ? metricUrls : Array.Empty<string>(),
                            startupAuthMode != StartupAuthenticationMode.NoAuth);
                    })
                    .UseStartup<Startup>();
                }).ConfigureHostConfiguration((IConfigurationBuilder builder) =>
                {
                    // Restore the Urls option so that it is readable in later configuration phases
                    if (!string.IsNullOrEmpty(aspnetUrls))
                    {
                        builder.AddInMemoryCollection(new Dictionary<string, string>()
                        {
                            { WebHostDefaults.ServerUrlsKey, aspnetUrls }
                        });
                    }
                }).ConfigureServices((HostBuilderContext context, IServiceCollection services) =>
                {
                    IAuthenticationConfigurator authConfigurator = AuthConfiguratorFactory.Create(startupAuthMode, context);
                    services.AddSingleton<IAuthenticationConfigurator>(authConfigurator);

                    //TODO Many of these service additions should be done through extension methods
                    services.AddSingleton(RealSystemClock.Instance);

                    services.AddSingleton<IEgressOutputConfiguration>(new EgressOutputConfiguration(httpEgressEnabled: !noHttpEgress));

                    authConfigurator.ConfigureApiAuth(services, context);

                    services.AddSwaggerGen(options =>
                    {
                        options.ConfigureMonitorSwaggerGen();
                        authConfigurator.ConfigureSwaggerGenAuth(options);
                    });

                    services.ConfigureDiagnosticPort(context.Configuration);

                    services.AddSingleton<OperationTrackerService>();

                    services.ConfigureGlobalCounter(context.Configuration);

                    services.ConfigureCollectionRuleDefaults(context.Configuration);

                    services.ConfigureTemplates(context.Configuration);

                    services.AddSingleton<IEndpointInfoSource, FilteredEndpointInfoSource>();
                    services.AddSingleton<ServerEndpointInfoSource>();
                    services.AddHostedServiceForwarder<ServerEndpointInfoSource>();
                    services.AddSingleton<IDiagnosticServices, DiagnosticServices>();
                    services.AddSingleton<IDumpService, DumpService>();
                    services.AddSingleton<IEndpointInfoSourceCallbacks, OperationTrackerServiceEndpointInfoSourceCallback>();
                    services.AddSingleton<IRequestLimitTracker, RequestLimitTracker>();
                    services.ConfigureOperationStore();
                    services.ConfigureEgress();
                    services.ConfigureMetrics(context.Configuration);
                    services.ConfigureStorage(context.Configuration);
                    services.ConfigureDefaultProcess(context.Configuration);
                    services.AddSingleton<ProfilerChannel>();
                    services.ConfigureCollectionRules();
                    services.ConfigureProfiler();
                    services.ConfigureStartupLoggers(authConfigurator);
                    services.AddSingleton<IExperimentalFlags, ExperimentalFlags>();
                    services.ConfigureInProcessFeatures(context.Configuration);
                    services.AddSingleton<IInProcessFeatures, InProcessFeatures>();
                    services.AddSingleton<IDumpOperationFactory, DumpOperationFactory>();
                    services.AddSingleton<ILogsOperationFactory, LogsOperationFactory>();
                    services.AddSingleton<IMetricsOperationFactory, MetricsOperationFactory>();
                    services.AddSingleton<ITraceOperationFactory, TraceOperationFactory>();
                });
        }
    }
}
