// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Microsoft.Diagnostics.Tools.Monitor.Swagger.Filters
{
    /// <summary>
    /// Adds an BadRequestResponse response component to the document.
    /// </summary>
    internal sealed class BadRequestResponseDocumentFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            OpenApiResponse unauthorizedResponse = new();
            unauthorizedResponse.Description = "Bad Request";
            unauthorizedResponse.Content.Add(
                ContentTypes.ApplicationProblemJson,
                new OpenApiMediaType()
                {
                    Schema = new OpenApiSchema()
                    {
                        Reference = new OpenApiReference()
                        {
                            Id = nameof(ValidationProblemDetails),
                            Type = ReferenceType.Schema
                        }
                    }
                });

            swaggerDoc.Components.Responses.Add(
                ResponseNames.BadRequestResponse,
                unauthorizedResponse);
        }
    }
}
