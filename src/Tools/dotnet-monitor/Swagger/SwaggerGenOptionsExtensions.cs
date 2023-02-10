// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Diagnostics.Monitoring.WebApi.Controllers;
using Microsoft.Diagnostics.Tools.Monitor.Swagger.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Diagnostics.Tools.Monitor.Swagger
{
    internal static class SwaggerGenOptionsExtensions
    {
        public static void ConfigureMonitorSwaggerGen(this SwaggerGenOptions options)
        {
            ConfigureMonitorSwaggerGenSecurity(options);

            options.DocumentFilter<BadRequestResponseDocumentFilter>();
            options.DocumentFilter<UnauthorizedResponseDocumentFilter>();
            options.DocumentFilter<TooManyRequestsResponseDocumentFilter>();

            options.OperationFilter<BadRequestResponseOperationFilter>();
            options.OperationFilter<RemoveFailureContentTypesOperationFilter>();
            options.OperationFilter<TooManyRequestsResponseOperationFilter>();
            options.OperationFilter<UnauthorizedResponseOperationFilter>();

            string documentationFile = $"{typeof(DiagController).Assembly.GetName().Name}.xml";
            string documentationPath = Path.Combine(AppContext.BaseDirectory, documentationFile);
            options.IncludeXmlComments(documentationPath);
        }

        private static void ConfigureMonitorSwaggerGenSecurity(this SwaggerGenOptions options)
        {
            const string ApiKeySecurityDefinitionName = "ApiKeyAuth";

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
        }
    }
}
