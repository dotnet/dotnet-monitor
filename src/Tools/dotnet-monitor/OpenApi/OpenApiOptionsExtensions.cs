// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Diagnostics.Monitoring.WebApi.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Diagnostics.Tools.Monitor.OpenApi.Transformers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.XPath;

namespace Microsoft.Diagnostics.Tools.Monitor.OpenApi
{
    internal static class OpenApiOptionsExtensions
    {
        public static void ConfigureMonitorOpenApiGen(this OpenApiOptions options)
        {
            options.AddDocumentTransformer<BadRequestResponseDocumentTransformer>();
            options.AddDocumentTransformer<UnauthorizedResponseDocumentTransformer>();
            options.AddDocumentTransformer<TooManyRequestsResponseDocumentTransformer>();

            options.AddOperationTransformer<BadRequestResponseOperationTransformer>();
//             options.OperationFilter<RemoveFailureContentTypesOperationFilter>();
            options.AddOperationTransformer<TooManyRequestsResponseOperationTransformer>();
            options.AddOperationTransformer<UnauthorizedResponseOperationTransformer>();

            options.AddSchemaTransformer((schema, context, cancellationToken) => {
                // FileResult should be represented as type: string, format: binary, not a ref.
                // if (schema.Reference != null && schema.Reference.Id == nameof(FileResult))
                if (context.JsonTypeInfo.Type == typeof(FileResult))
                {
                    // if (string.Empty.Length == 0) {
                    //     throw new System.Exception("FileResuiLT!");
                    // }
                    schema.Reference = null;
                    schema.Type = "string";
                    schema.Format = "binary";
                    schema.Properties = new Dictionary<string, OpenApiSchema>();
                }
                return Task.CompletedTask;
            });

            options.CreateSchemaReferenceId = (type) => {
                if (type.Type == typeof(FileResult)) {
                    // always inline.
                    return null;
                }
                return OpenApiOptions.CreateDefaultSchemaReferenceId(type);
            };

            // options.CreateSchemaReferenceId = (type) => {
            //     Console.WriteLine("Create schema ref id for type " + type.ToString());
            //     return type.Type.IsEnum ? null : OpenApiOptions.CreateDefaultSchemaReferenceId(type);
            // };

            // options.AddDocumentTransformer((document, context, cancellationToken) => {
            //     // Get rid of any 400 Bad Request section, replacing it with a reference
            //     // to components/responses/BadRequestResponse.
            //     if (document.Paths != null)
            //     {
            //         foreach (var path in document.Paths)
            //         {
            //             foreach (var operation in path.Value.Operations)
            //             {
            //                 if (operation.Value.Responses.ContainsKey("400"))
            //                 {
            //                     operation.Value.Responses["400"] = new OpenApiResponse
            //                     {
            //                         Reference = new OpenApiReference
            //                         {
            //                             Type = ReferenceType.Response,
            //                             Id = "BadRequestResponse"
            //                         }
            //                     };
            //                 }
            //             }
            //         }
            //     }
            //     // Add the common response
            //     document.Components.Responses.Add("BadRequestResponse", new OpenApiResponse
            //     {
            //         Description = "Bad Request",
            //         Content = new Dictionary<string, OpenApiMediaType>
            //         {
            //             { ContentTypes.ApplicationProblemJson, new OpenApiMediaType() }
            //         }
            //     });
            // });




//             string documentationFile = $"{typeof(DiagController).Assembly.GetName().Name}.xml";
// #nullable disable
//             options.IncludeXmlComments(() => new XPathDocument(Assembly.GetExecutingAssembly().GetManifestResourceStream(documentationFile)));
// #nullable restore
//             // Make sure TimeSpan is represented as a string instead of a full object type
//             options.MapType<TimeSpan>(() => new OpenApiSchema() { Type = "string", Format = "time-span", Example = new OpenApiString("00:00:30") });
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
