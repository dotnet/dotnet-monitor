// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

            // This is needed to allow the StreamingLogger to synchronously write to the output stream.
            // Eventually should switch StreamingLoggger to something that allows for async operations.
            services.Configure<KestrelServerOptions>(options =>
            {
                options.AllowSynchronousIO = true;
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
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
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
    }
}
