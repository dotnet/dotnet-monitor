// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Tools.Monitor.Swagger.Filters
{
    /// <summary>
    /// Adds an BadRequestResponse response component to the document.
    /// </summary>
    internal sealed class BadRequestResponseDocumentFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            OpenApiResponse badRequestResponse = new();
            badRequestResponse.Description = "Bad Request";
            badRequestResponse.Content ??= new Dictionary<string, OpenApiMediaType>();
            badRequestResponse.Content.Add(
                ContentTypes.ApplicationProblemJson,
                new OpenApiMediaType()
                {
                    Schema = new OpenApiSchemaReference(nameof(ValidationProblemDetails), hostDocument: null)
                });

            swaggerDoc.Components ??= new OpenApiComponents();
            swaggerDoc.Components.Responses ??= new Dictionary<string, IOpenApiResponse>();
            swaggerDoc.Components.Responses.Add(
                ResponseNames.BadRequestResponse,
                badRequestResponse);
        }
    }
}
