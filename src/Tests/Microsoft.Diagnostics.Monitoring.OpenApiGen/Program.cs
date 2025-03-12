// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Diagnostics.Tools.Monitor.Auth;
using Microsoft.Diagnostics.Tools.Monitor.OpenApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Diagnostics.Monitoring.WebApi;
// using Swashbuckle.AspNetCore.Swagger;
// using System;
// using System.Globalization;
// using System.IO;

namespace Microsoft.Diagnostics.Monitoring.OpenApiGen
{
    internal sealed class Program
    {
        public static void Main(string[] args)
        {
            // if (args.Length != 1)
            // {
            //     throw new InvalidOperationException("Expected single argument for the output path.");
            // }
            // string outputPath = args[0];

            // Create directory if it does not exist
            // Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

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
                    // services.AddSwaggerGen(options => options.ConfigureMonitorSwaggerGen());
                    services.AddOpenApi(options => options.ConfigureMonitorOpenApiGen());
                })
                .Build();

            var config = host.Services.GetRequiredService<IConfiguration>();
            var startup = new Startup(config);
            var appBuilder = new ApplicationBuilder(host.Services);
            var env = host.Services.GetRequiredService<IWebHostEnvironment>();
            var corsOptions = host.Services.GetRequiredService<IOptions<CorsConfigurationOptions>>();
            Startup.Configure(appBuilder, env, corsOptions);

            // Necessary to call Configure, which does MapOpenApi.
            // Run the app!
            // host.Run();

            // Serialize the OpenApi document
            // using StringWriter outputWriter = new(CultureInfo.InvariantCulture);
            // ISwaggerProvider provider = host.Services.GetRequiredService<ISwaggerProvider>();
            // provider.WriteTo(outputWriter);
            // outputWriter.Flush();

            // Normalize line endings before writinge
            // File.WriteAllText(outputPath, outputWriter.ToString().Replace("\r\n", "\n"));
        }
    }
}
