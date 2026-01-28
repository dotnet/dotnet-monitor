// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Tools.Monitor.Swagger.Filters
{
    /// <summary>
    /// Adds an UnauthorizedResponse response component to the document.
    /// </summary>
    internal sealed class UnauthorizedResponseDocumentFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            OpenApiHeader authenticateHeader = new();
            authenticateHeader.Schema = new OpenApiSchema() { Type = JsonSchemaType.String };

            OpenApiResponse unauthorizedResponse = new();
            unauthorizedResponse.Description = "Unauthorized";
            unauthorizedResponse.Headers ??= new Dictionary<string, IOpenApiHeader>();
            unauthorizedResponse.Headers.Add("WWW_Authenticate", authenticateHeader);

            swaggerDoc.Components ??= new OpenApiComponents();
            swaggerDoc.Components.Responses ??= new Dictionary<string, IOpenApiResponse>();
            swaggerDoc.Components.Responses.Add(
                ResponseNames.UnauthorizedResponse,
                unauthorizedResponse);
        }
    }
}
