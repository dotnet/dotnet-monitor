// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Diagnostics.Tools.Monitor.Auth;
using Microsoft.Diagnostics.Tools.Monitor.Swagger;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;
using Swashbuckle.AspNetCore.Swagger;
using System;
using System.Globalization;
using System.IO;

namespace Microsoft.Diagnostics.Monitoring.OpenApiGen
{
    internal sealed class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                throw new InvalidOperationException("Expected single argument for the output path.");
            }
            string outputPath = args[0];

            // Create directory if it does not exist
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

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
                    services.AddSwaggerGen(options => options.ConfigureMonitorSwaggerGen());
                })
                .Build();

            // Generate the OpenAPI document
            OpenApiDocument document = host.Services
                .GetRequiredService<ISwaggerProvider>()
                .GetSwagger("v1");

            // Serialize the document to the file
            using StringWriter outputWriter = new(CultureInfo.InvariantCulture);
            document.SerializeAsV3(new OpenApiJsonWriter(outputWriter));
            outputWriter.Flush();

            // Normalize line endings before writing
            File.WriteAllText(outputPath, outputWriter.ToString().Replace("\r\n", "\n"));
        }
    }
}
