// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Diagnostics.Monitoring;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text.Json.Serialization;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // AddControllers is sufficient because the tool does not use Razor nor Views.
            services.AddControllers()
            .AddJsonOptions(options =>
            {
                // Allow serialization of enum values into strings rather than numbers.
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            })
            .AddApplicationPart(typeof(DiagController).Assembly);

            services.AddControllers(options =>
            {
                options.Filters.Add(typeof(EgressValidationUnhandledExceptionFilter));
            });

            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var details = new ValidationProblemDetails(context.ModelState);
                    var result = new BadRequestObjectResult(details);
                    result.ContentTypes.Add(ContentTypes.ApplicationProblemJson);
                    return result;
                };
            });

            services.Configure<BrotliCompressionProviderOptions>(options =>
            {
                options.Level = CompressionLevel.Optimal;
            });

            services.AddResponseCompression(configureOptions =>
            {
                configureOptions.Providers.Add<BrotliCompressionProvider>();
                configureOptions.MimeTypes = new List<string> { ContentTypes.ApplicationOctetStream };
            });

            var metricsOptions = new MetricsOptions();
            Configuration.Bind(ConfigurationKeys.Metrics, metricsOptions);
            if (metricsOptions.Enabled.GetValueOrDefault(MetricsOptionsDefaults.Enabled))
            {
                services.AddSingleton<MetricsStoreService>();
                services.AddHostedService<MetricsService>();
            }

            services.AddSingleton<IMetricsPortsProvider, MetricsPortsProvider>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(
            IApplicationBuilder app,
            IHostApplicationLifetime lifetime,
            IWebHostEnvironment env,
            IAuthConfiguration options,
            AddressListenResults listenResults,
            MonitorApiKeyConfigurationObserver optionsObserver,
            HostBuilderContext hostBuilderContext,
            ILogger<Startup> logger)
        {
            logger.ExperienceSurvey();

            // These errors are populated before Startup.Configure is called because
            // the KestrelServer class is configured as a prerequisite of
            // GenericWebHostServer being instantiated. The GenericWebHostServer invokes
            // Startup.Configure as part of its StartAsync method. This method is the 
            // first opportunity to log anything through ILogger (a dedicated HostedService
            // could be written for this, but there is no guarantee that service would run
            // after the GenericWebHostServer is instantiated but before it is started).
            foreach (AddressListenResult result in listenResults.Errors)
            {
                logger.UnableToListenToAddress(result.Url, result.Exception);
            }

            if (hostBuilderContext.Properties.TryGetValue(HostBuilderResults.ResultKey, out object resultsObject))
            {
                if (resultsObject is HostBuilderResults hostBuilderResults)
                {
                    foreach (string message in hostBuilderResults.Warnings)
                    {
                        logger.LogWarning(message);
                    }
                }
            }

            // If we end up not listening on any ports, Kestrel defaults to port 5000. Make sure we don't attempt this.
            // Startup.Configure is called before KestrelServer is started
            // by the GenericWebHostServer, so there is no duplication of logging errors
            // and Kestrel does not bind to default ports.
            if (!listenResults.AnyAddresses)
            {
                // This is logged by GenericWebHostServer.StartAsync
                throw new MonitoringException(Strings.ErrorMessage_UnableToBindUrls);
            }

            lifetime.ApplicationStarted.Register(() => LogBoundAddresses(app.ServerFeatures, listenResults, logger));

            LogElevatedPermissions(options, logger);

            // Start listening for options changes so they can be logged when changed.
            optionsObserver.Initialize();

            if (options.KeyAuthenticationMode == KeyAuthenticationMode.NoAuth)
            {
                logger.NoAuthentication();
            }
            else
            {
                if (options.KeyAuthenticationMode == KeyAuthenticationMode.TemporaryKey)
                {
                    logger.LogTempKey(options.TemporaryJwtKey.Token);
                }
                //Auth is enabled and we are binding on http. Make sure we log a warning.

                string hostingUrl = Configuration.GetValue<string>(WebHostDefaults.ServerUrlsKey);
                string[] urls = ConfigurationHelper.SplitValue(hostingUrl);
                foreach (string url in urls)
                {
                    BindingAddress address = null;
                    try
                    {
                        address = BindingAddress.Parse(url);
                    }
                    catch (FormatException ex)
                    {
                        logger.ParsingUrlFailed(url, ex);
                        continue;
                    }

                    if (string.Equals(Uri.UriSchemeHttp, address.Scheme, StringComparison.OrdinalIgnoreCase))
                    {
                        logger.InsecureAuthenticationConfiguration();
                        break;
                    }
                }
            }

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            CorsConfigurationOptions corsConfiguration = new CorsConfigurationOptions();
            Configuration.Bind(ConfigurationKeys.CorsConfiguration, corsConfiguration);
            if (!string.IsNullOrEmpty(corsConfiguration.AllowedOrigins))
            {
                app.UseCors(builder => builder.WithOrigins(corsConfiguration.GetOrigins()).AllowAnyHeader().AllowAnyMethod());
            }

            // Disable response compression due to ASP.NET 6.0 bug:
            // https://github.com/dotnet/aspnetcore/issues/36960
            //app.UseResponseCompression();

            //Note this must be after UseRouting but before UseEndpoints
            app.UseMiddleware<RequestLimitMiddleware>();

            app.UseEndpoints(builder =>
            {
                builder.MapControllers();
            });
        }

        private static void LogBoundAddresses(IFeatureCollection features, AddressListenResults results, ILogger logger)
        {
            IServerAddressesFeature serverAddresses = features.Get<IServerAddressesFeature>();

            // This logging allows the tool to differentiate which addresses
            // are default address and which are metrics addresses.

            foreach (string defaultAddress in results.GetDefaultAddresses(serverAddresses))
            {
                logger.BoundDefaultAddress(defaultAddress);
            }

            foreach (string metricAddress in results.GetMetricsAddresses(serverAddresses))
            {
                logger.BoundMetricsAddress(metricAddress);
            }
        }

        private static void LogElevatedPermissions(IAuthConfiguration options, ILogger logger)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                WindowsIdentity currentUser = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(currentUser);
                if (principal.IsInRole(WindowsBuiltInRole.Administrator))
                {
                    logger.RunningElevated();
                    // In the future this will need to be modified when ephemeral keys are setup
                    if (options.EnableNegotiate)
                    {
                        logger.DisabledNegotiateWhileElevated();
                    }
                }
            }

            // in the future we should check that we aren't running root on linux (out of scope for now)
        }
    }
}
