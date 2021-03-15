// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Diagnostics.Monitoring.RestServer.Controllers;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;
using Swashbuckle.AspNetCore.Swagger;
using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace Microsoft.Diagnostics.Monitoring.OpenApiGen
{
    internal sealed class Program
    {
        private static readonly OpenApiSchema Int32Schema =
            new OpenApiSchema() { Type = "integer", Format = "int32" };
        private static readonly OpenApiSchema GuidSchema =
            new OpenApiSchema() { Type = "string", Format = "uuid" };

        private static Func<OpenApiSchema> CreateProcessKeySchema =>
            () => new OpenApiSchema() { OneOf = { Int32Schema, GuidSchema } };

        public static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                throw new InvalidOperationException("Expected single argument for the output path.");
            }
            string outputPath = args[0];
            
            // Create directory if it does not exist
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            // Create all of the same services as dotnet-monitor and add
            // OpenAPI generation in order to have it inspect the ASP.NET Core
            // registrations and descriptions.
            IHost host = DiagnosticsMonitorCommandHandler
                .CreateHostBuilder(
                    console: null,
                    urls: Array.Empty<string>(),
                    metricUrls: Array.Empty<string>(),
                    metrics: true,
                    diagnosticPort: null,
                    noAuth: false)
                .ConfigureServices(services =>
                {
                    services.AddSwaggerGen(options =>
                    {
                        options.MapType<ProcessKey>(CreateProcessKeySchema);
                        options.MapType<ProcessKey?>(CreateProcessKeySchema);

                        options.DocumentFilter<BadRequestResponseDocumentFilter>();
                        options.DocumentFilter<UnauthorizedResponseDocumentFilter>();
                        options.DocumentFilter<TooManyRequestsResponseDocumentFilter>();

                        options.OperationFilter<BadRequestResponseOperationFilter>();
                        options.OperationFilter<RemoveFailureContentTypesOperationFilter>();
                        options.OperationFilter<TooManyRequestsResponseOperationFilter>();
                        options.OperationFilter<UnauthorizedResponseOperationFilter>();

                        var documentationFile = $"{typeof(DiagController).Assembly.GetName().Name}.xml";
                        var documentationPath = Path.Combine(AppContext.BaseDirectory, documentationFile);
                        options.IncludeXmlComments(documentationPath);
                    });
                })
                .Build();

            // Generate the OpenAPI document
            OpenApiDocument document = host.Services
                .GetRequiredService<ISwaggerProvider>()
                .GetSwagger("v1");

            // Serialize the document to the file
            using StringWriter outputWriter = new StringWriter(CultureInfo.InvariantCulture);
            document.SerializeAsV3(new OpenApiJsonWriter(outputWriter));
            outputWriter.Flush();

            // Normalize line endings before writing
            File.WriteAllText(outputPath, outputWriter.ToString().Replace("\r\n", "\n"));
        }
    }
}
