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
using System.Reflection;
using System.Xml.XPath;

namespace Microsoft.Diagnostics.Tools.Monitor.Swagger
{
    internal static class SwaggerGenOptionsExtensions
    {
        public static void ConfigureMonitorSwaggerGen(this SwaggerGenOptions options)
        {
            options.DocumentFilter<BadRequestResponseDocumentFilter>();
            options.DocumentFilter<UnauthorizedResponseDocumentFilter>();
            options.DocumentFilter<TooManyRequestsResponseDocumentFilter>();

            options.OperationFilter<BadRequestResponseOperationFilter>();
            options.OperationFilter<RemoveFailureContentTypesOperationFilter>();
            options.OperationFilter<TooManyRequestsResponseOperationFilter>();
            options.OperationFilter<UnauthorizedResponseOperationFilter>();

            string documentationFile = $"{typeof(DiagController).Assembly.GetName().Name}.xml";
            options.IncludeXmlComments(() => new XPathDocument(Assembly.GetExecutingAssembly().GetManifestResourceStream(documentationFile)));
        }

        public static void AddBearerTokenAuthOption(this SwaggerGenOptions options, string securityDefinitionName)
        {
            options.AddSecurityDefinition(securityDefinitionName, new OpenApiSecurityScheme
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
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = securityDefinitionName }
                    },
                    Array.Empty<string>()
                }
            });
        }
    }
}
