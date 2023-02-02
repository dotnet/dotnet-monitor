// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Diagnostics.Monitoring.WebApi.Controllers;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;
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
        private const string ApiKeySecurityDefinitionName = "ApiKeyAuth";

        private static readonly OpenApiSchema ProcessKey_Int32Schema =
            new OpenApiSchema() { Type = "integer", Format = "int32", Description = "The ID of the process." };
        private static readonly OpenApiSchema ProcessKey_GuidSchema =
            new OpenApiSchema() { Type = "string", Format = "uuid", Description = "The runtime instance cookie of the runtime." };
        private static readonly OpenApiSchema ProcessKey_StringSchema =
            new OpenApiSchema() { Type = "string", Description = "The name of the process." };

        private static Func<OpenApiSchema> CreateProcessKeySchema =>
            () => new OpenApiSchema() { OneOf = { ProcessKey_Int32Schema, ProcessKey_GuidSchema, ProcessKey_StringSchema } };

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
                authConfiguration: HostBuilderHelper.CreateAuthConfiguration(noAuth: false, tempApiKey: false),
                userProvidedConfigFilePath: null);

            // Create all of the same services as dotnet-monitor and add
            // OpenAPI generation in order to have it inspect the ASP.NET Core
            // registrations and descriptions.
            IHost host = HostBuilderHelper
                .CreateHostBuilder(settings)
                .ConfigureServices(services =>
                {
                    services.AddSwaggerGen(options =>
                    {
                        options.AddSecurityDefinition(ApiKeySecurityDefinitionName, new OpenApiSecurityScheme
                        {
                            Name = HeaderNames.Authorization,
                            Type = SecuritySchemeType.ApiKey,
                            Scheme = JwtBearerDefaults.AuthenticationScheme,
                            BearerFormat = "JWT",
                            In = ParameterLocation.Header,
                            Description = Strings.HelpDescription_SecurityDefinitionDescription_ApiKey
                        });

                        options.AddSecurityRequirement(new OpenApiSecurityRequirement
                        {
                            {
                                new OpenApiSecurityScheme
                                {
                                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = ApiKeySecurityDefinitionName }
                                },
                                Array.Empty<string>()
                            }
                        });

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
            using StringWriter outputWriter = new(CultureInfo.InvariantCulture);
            document.SerializeAsV3(new OpenApiJsonWriter(outputWriter));
            outputWriter.Flush();

            // Normalize line endings before writing
            File.WriteAllText(outputPath, outputWriter.ToString().Replace("\r\n", "\n"));
        }
    }
}
