// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Diagnostics.Monitoring;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.Auth;
using Microsoft.Diagnostics.Tools.Monitor.Stacks;
using Microsoft.Diagnostics.Tools.Monitor.Swagger;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.Commands
{
    internal static class CollectCommandHandler
    {
        public static async Task<int> Invoke(CancellationToken token, string[]? urls, string[]? metricUrls, bool metrics, string? diagnosticPort, bool noAuth, bool tempApiKey, bool noHttpEgress, FileInfo? configurationFilePath, bool exitOnStdinDisconnect)
        {
            try
            {
                StartupAuthenticationMode authMode = HostBuilderHelper.GetStartupAuthenticationMode(noAuth, tempApiKey);
                HostBuilderSettings settings = HostBuilderSettings.CreateMonitor(urls, metricUrls, metrics, diagnosticPort, authMode, configurationFilePath);

                IHost host = HostBuilderHelper.CreateHostBuilder(settings)
                    .Configure(authMode, noHttpEgress, settings)
                    .Build();

                try
                {
                    if (exitOnStdinDisconnect)
                    {
                        WatchStdinForDisconnect(host.Services, token);
                    }

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

        private static IHostBuilder Configure(this IHostBuilder builder, StartupAuthenticationMode startupAuthMode, bool noHttpEgress, HostBuilderSettings settings)
        {
            return builder.ConfigureServices((HostBuilderContext context, IServiceCollection services) =>
            {
                IAuthenticationConfigurator authConfigurator = AuthConfiguratorFactory.Create(startupAuthMode, context);
                services.AddSingleton<IAuthenticationConfigurator>(authConfigurator);

                //TODO Many of these service additions should be done through extension methods
                services.AddSingleton(TimeProvider.System);

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

                services.ConfigureDotnetMonitorDebug(context.Configuration);

                services.ConfigureEndpointInfoSource();

                services.AddSingleton<IDiagnosticServices, DiagnosticServices>();
                services.AddSingleton<IDumpService, DumpService>();
                services.AddSingleton<IEndpointInfoSourceCallbacks, OperationTrackerServiceEndpointInfoSourceCallback>();
                services.ConfigureRequestLimits();
                services.ConfigureOperationStore();
                services.ConfigureExtensions();
                services.ConfigureExtensionLocations(settings);
                services.ConfigureEgress();
                services.ConfigureMetrics(context.Configuration);
                services.ConfigureParameterCapturing();
                services.ConfigureStorage(context.Configuration);
                services.ConfigureDefaultProcess(context.Configuration);
                services.AddSingleton<ProfilerChannel>();
                services.ConfigureCollectionRules();
                services.ConfigureLibrarySharing();

                // 
                // The order of the below calls is **important**.
                // - ConfigureInProcessFeatures needs to be called before ConfigureProfiler and ConfigureStartupHook
                //   because these features will configure themselves depending on environment variables set by InProcessFeaturesEndpointInfoSourceCallbacks.
                // - ConfigureProfiler needs to be called before ConfigureStartupHook
                //   because the startup hook may call into the profiler on load.
                // - ConfigureExceptions needs to be called before ConfigureStartupHook
                //   because we want to avoid missing exception data events and potentially having an out-of-sync name cache.
                //
                services.ConfigureInProcessFeatures(context.Configuration);
                services.ConfigureProfiler();
                services.ConfigureExceptions();
                services.ConfigureStartupHook();

                services.ConfigureStartupLoggers(authConfigurator);
                services.AddSingleton<IInProcessFeatures, InProcessFeatures>();
                services.AddSingleton<IDumpOperationFactory, DumpOperationFactory>();
                services.AddSingleton<ILogsOperationFactory, LogsOperationFactory>();
                services.AddSingleton<IMetricsOperationFactory, MetricsOperationFactory>();
                services.AddSingleton<ITraceOperationFactory, TraceOperationFactory>();
                services.AddSingleton<IGCDumpOperationFactory, GCDumpOperationFactory>();
                services.AddSingleton<IStacksOperationFactory, StacksOperationFactory>();

                // Per-process services must be scoped
                services.AddScoped<ScopedEndpointInfo>();
                services.AddScopedForwarder<IEndpointInfo, ScopedEndpointInfo>();
            })
            .ConfigureContainer((HostBuilderContext context, IServiceCollection services) =>
            {
                ServerUrlsBlockingConfigurationManager? manager =
                    context.Properties[typeof(ServerUrlsBlockingConfigurationManager)] as ServerUrlsBlockingConfigurationManager;
                Debug.Assert(null != manager, $"Expected {typeof(ServerUrlsBlockingConfigurationManager).FullName} to be a {typeof(HostBuilderContext).FullName} property.");
                if (null != manager)
                {
                    // Block reading of the Urls option so that Kestrel is unable to read it from the composed configuration.
                    manager.IsBlocking = true;
                }
            });
        }

        private static void WatchStdinForDisconnect(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            IHostApplicationLifetime lifetime;
            Stream inputStream;
            try
            {
                lifetime = serviceProvider.GetRequiredService<IHostApplicationLifetime>();
                inputStream = Console.OpenStandardInput();
            }
            catch (Exception e)
            {
                serviceProvider.GetRequiredService<ILoggerFactory>()
                    .CreateLogger(typeof(CollectCommandHandler))
                    .UnableToWatchForDisconnect(e);

                throw new MonitoringException(Strings.LogFormatString_UnableToWatchForDisconnect);
            }

            async Task WatchAsync()
            {
                byte[] buffer = new byte[100];
                try
                {
                    while (true)
                    {
                        if (await inputStream.ReadAsync(buffer.AsMemory(), cancellationToken) == 0)
                        {
                            // input stream closed
                            break;
                        }
                    }
                }
                catch (IOException)
                {
                    // In case the input stream returns a OS error, we will just exit.
                }
                catch (UnauthorizedAccessException)
                {
                    // In case the input stream returns ERROR_ACCESS_DENIED, we will just exit.
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                lifetime.StopApplication();
            }

            // Kick off asynchronously reading from stdin
            _ = WatchAsync();
        }
    }
}
