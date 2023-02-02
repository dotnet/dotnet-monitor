// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Diagnostics.Monitoring;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                AuthConfiguration authConfiguration = HostBuilderHelper.CreateAuthConfiguration(noAuth, tempApiKey);
                HostBuilderSettings settings = HostBuilderSettings.CreateMonitor(urls, metricUrls, metrics, diagnosticPort, authConfiguration, configurationFilePath);

                IHost host = HostBuilderHelper.CreateHostBuilder(settings)
                    .Configure(authConfiguration, noHttpEgress, settings)
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

        private static IHostBuilder Configure(this IHostBuilder builder, AuthConfiguration authenticationOptions, bool noHttpEgress, HostBuilderSettings settings)
        {
            return builder.ConfigureServices((HostBuilderContext context, IServiceCollection services) =>
            {
                //TODO Many of these service additions should be done through extension methods
                services.AddSingleton(RealSystemClock.Instance);

                services.AddSingleton<IAuthConfiguration>(authenticationOptions);

                services.AddSingleton<IEgressOutputConfiguration>(new EgressOutputConfiguration(httpEgressEnabled: !noHttpEgress));

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
                services.ConfigureExtensions(settings);
                services.ConfigureEgress();
                services.ConfigureMetrics(context.Configuration);
                services.ConfigureStorage(context.Configuration);
                services.ConfigureDefaultProcess(context.Configuration);
                services.AddSingleton<ProfilerChannel>();
                services.ConfigureCollectionRules();
                services.ConfigureProfiler();
                services.ConfigureStartupLoggers();
                services.AddSingleton<IExperimentalFlags, ExperimentalFlags>();
                services.ConfigureInProcessFeatures(context.Configuration);
                services.AddSingleton<IInProcessFeatures, InProcessFeatures>();
                services.AddSingleton<IDumpOperationFactory, DumpOperationFactory>();
                services.AddSingleton<ILogsOperationFactory, LogsOperationFactory>();
                services.AddSingleton<IMetricsOperationFactory, MetricsOperationFactory>();
                services.AddSingleton<ITraceOperationFactory, TraceOperationFactory>();
            })
            .ConfigureContainer((HostBuilderContext context, IServiceCollection services) =>
            {
                ServerUrlsBlockingConfigurationManager manager =
                    context.Properties[typeof(ServerUrlsBlockingConfigurationManager)] as ServerUrlsBlockingConfigurationManager;
                Debug.Assert(null != manager, $"Expected {typeof(ServerUrlsBlockingConfigurationManager).FullName} to be a {typeof(HostBuilderContext).FullName} property.");
                if (null != manager)
                {
                    // Block reading of the Urls option so that Kestrel is unable to read it from the composed configuration.
                    manager.IsBlocking = true;
                }
            });
        }
    }
}
