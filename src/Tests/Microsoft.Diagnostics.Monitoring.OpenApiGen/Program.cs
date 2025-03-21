// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Diagnostics.Tools.Monitor.Auth;
using Microsoft.Diagnostics.Tools.Monitor.OpenApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.OpenApiGen
{
    internal sealed class Program
    {
        private static async Task Main(string[] args)
        {
            if (args.Length != 1)
            {
                throw new InvalidOperationException("Expected single argument for the output path.");
            }
            string outputPath = args[0];

            // Create directory if it does not exist
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

            HostBuilderSettings settings = HostBuilderSettings.CreateMonitor(
                urls: null,
                metricUrls: null,
                metrics: false,
                diagnosticPort: null,
                startupAuthMode: StartupAuthenticationMode.Deferred,
                userProvidedConfigFilePath: null);

            // Create all of the same services as dotnet-monitor and add
            // OpenAPI generation in order to have it inspect the ASP.NET Core
            // registrations and descriptions.
            IHost host = HostBuilderHelper
                .CreateHostBuilder(settings)
                .ConfigureServices(services =>
                {
                    services.AddOpenApi(options => options.ConfigureMonitorOpenApiGen());
                })
                .Build();

            // Ensure that Startup.Configure is called, to add endpoints
            var config = host.Services.GetRequiredService<IConfiguration>();
            var startup = new Startup(config);
            var appBuilder = new ApplicationBuilder(host.Services);
            var env = host.Services.GetRequiredService<IWebHostEnvironment>();
            var corsOptions = host.Services.GetRequiredService<IOptions<CorsConfigurationOptions>>();
            Startup.Configure(appBuilder, env, corsOptions);

            var openApiDocument = await GetOpenApiDocument(host);

            // Serialize the OpenApi document
            using FileStream stream = File.Create(outputPath);
            using StreamWriter writer = new(stream);
            var openApiWriter = new OpenApiJsonWriter(writer);
            openApiDocument.SerializeAsV3(openApiWriter);
        }

        private static object GetDocumentService(IServiceProvider serviceProvider)
        {
            var serviceType = Type.GetType("Microsoft.AspNetCore.OpenApi.OpenApiDocumentService, Microsoft.AspNetCore.OpenApi", throwOnError: true)!;
            return serviceProvider.GetRequiredKeyedService(serviceType, "v1")!;

        }

        private static async Task<OpenApiDocument> GetOpenApiDocument(IHost host)
        {
            var documentService = GetDocumentService(host.Services);
            var methodInfo = documentService.GetType().GetMethod("GetOpenApiDocumentAsync", BindingFlags.Public | BindingFlags.Instance)!;

            object result = methodInfo.Invoke(documentService, new object?[] { host.Services, default(CancellationToken) })!;

            return await (Task<OpenApiDocument>)result;
        }
    }
}
