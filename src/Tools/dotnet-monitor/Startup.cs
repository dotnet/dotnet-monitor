// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Monitoring.WebApi.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.IO.Compression;
using System.Text.Json.Serialization;
using System.Reflection;
using Microsoft.AspNetCore.Http.Validation.Generated;

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
            services.ConfigureHttpJsonOptions(options => {
                options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.SerializerOptions.TypeInfoResolverChain.Add(MonitorJsonSerializerContext.Default);
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

            app.UseRouting();

            app.UseMiddleware<HostRestrictionMiddleware>();

            app.UseAuthentication();
            if (Assembly.GetEntryAssembly()?.GetName().Name != "Microsoft.Diagnostics.Monitoring.OpenApiGen")
            {
                app.UseAuthorization();
            }

            if (!string.IsNullOrEmpty(corsOptions.Value.AllowedOrigins))
            {
                app.UseCors(builder => builder.WithOrigins(corsOptions.Value.GetOrigins()).AllowAnyHeader().AllowAnyMethod());
            }

            // Disable response compression due to ASP.NET 6.0 bug:
            // https://github.com/dotnet/aspnetcore/issues/36960
            //app.UseResponseCompression();

            app.UseEndpoints(builder =>
            {
                var serviceProvider = builder.ServiceProvider;

                DiagController.MapActionMethods(builder);
                DiagController.MapMetricsActionMethods(builder);
                ExceptionsController.MapActionMethods(builder);
                MetricsController.MapActionMethods(builder);
                OperationsController.MapActionMethods(builder);

                builder.MapOpenApi("/");

                app.UseMiddleware<EgressValidationUnhandledExceptionMiddleware>();
            });
        }
    }
}
