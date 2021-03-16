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
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Diagnostics.Monitoring;
using Microsoft.Diagnostics.Monitoring.RestServer;
using Microsoft.Diagnostics.Monitoring.RestServer.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO.Compression;
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
            .SetCompatibilityVersion(CompatibilityVersion.Latest)
            .AddApplicationPart(typeof(DiagController).Assembly);

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
                configureOptions.MimeTypes = new List<string> { ContentTypes.ApplicationOctectStream };
            });

            // This is needed to allow the StreamingLogger to synchronously write to the output stream.
            // Eventually should switch StreamingLoggger to something that allows for async operations.
            services.Configure<KestrelServerOptions>(options =>
            {
                options.AllowSynchronousIO = true;
            });

            var metricsOptions = new MetricsOptions();
            Configuration.Bind(MetricsOptions.ConfigurationKey, metricsOptions);
            if (metricsOptions.Enabled)
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
            ExperimentalToolLogger exprLogger,
            IAuthOptions options,
            AddressListenResults listenResults,
            ILogger<Startup> logger)
        {
            exprLogger.LogExperimentMessage();

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

            // If we end up not listening on any ports, Kestrel defaults to port 5000. Make sure we don't attempt this.
            // Startup.Configure is called before KestrelServer is started
            // by the GenericWebHostServer, so there is no duplication of logging errors
            // and Kestrel does not bind to default ports.
            if (!listenResults.AnyAddresses)
            {
                // This is logged by GenericWebHostServer.StartAsync
                throw new MonitoringException("Unable to bind any urls.");
            }

            lifetime.ApplicationStarted.Register(() => LogBoundAddresses(app.ServerFeatures, listenResults, logger));

            if (options.KeyAuthenticationMode == KeyAuthenticationMode.NoAuth)
            {
                logger.NoAuthentication();
            }
            else
            {
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
                    catch (Exception)
                    {
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

            CorsConfiguration corsConfiguration = new CorsConfiguration();
            Configuration.Bind(nameof(CorsConfiguration), corsConfiguration);
            if (!string.IsNullOrEmpty(corsConfiguration.AllowedOrigins))
            {
                app.UseCors(builder => builder.WithOrigins(corsConfiguration.GetOrigins()).AllowAnyHeader().AllowAnyMethod());
            }

            app.UseResponseCompression();

            //Note this must be after UseRouting but before UseEndpoints
            app.UseMiddleware<Throttling>();

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
    }
}
