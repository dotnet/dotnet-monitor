﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Controllers;
using Microsoft.Diagnostics.Tools.Monitor.Swagger;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Swagger;
using System.Collections.Generic;
using System.IO;
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
            services.ConfigureHttpJsonOptions(options => {
                options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
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

            services.ConfigureCors(Configuration);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public static void Configure(IApplicationBuilder app, IWebHostEnvironment env, IOptions<CorsConfigurationOptions> corsOptions)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseSwagger();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            if (!string.IsNullOrEmpty(corsOptions.Value.AllowedOrigins))
            {
                app.UseCors(builder => builder.WithOrigins(corsOptions.Value.GetOrigins()).AllowAnyHeader().AllowAnyMethod());
            }

            // Disable response compression due to ASP.NET 6.0 bug:
            // https://github.com/dotnet/aspnetcore/issues/36960
            //app.UseResponseCompression();

            app.UseEndpoints(builder =>
            {
                builder.MapControllers();

                var serviceProvider = builder.ServiceProvider;

                new OperationsController(serviceProvider.GetRequiredService<ILogger<OperationsController>>(), serviceProvider)
                    .MapActionMethods(builder);

                new DiagController(serviceProvider, serviceProvider.GetRequiredService<ILogger<DiagController>>())
                    .MapActionMethods(builder)
                    .MapMetricsActionMethods(builder);

                new ExceptionsController(serviceProvider, serviceProvider.GetRequiredService<ILogger<ExceptionsController>>())
                    .MapActionMethods(builder);

                new MetricsController(serviceProvider.GetRequiredService<ILogger<MetricsController>>(), serviceProvider, serviceProvider.GetRequiredService<IOptions<MetricsOptions>>())
                    .MapActionMethods(builder);

                builder.MapGet("/", (HttpResponse response, ISwaggerProvider provider) =>
                {
                    using Stream stream = response.BodyWriter.AsStream(true);

                    provider.WriteTo(stream);
                });

                app.UseMiddleware<EgressValidationUnhandledExceptionMiddleware>();
            });
        }
    }
}
