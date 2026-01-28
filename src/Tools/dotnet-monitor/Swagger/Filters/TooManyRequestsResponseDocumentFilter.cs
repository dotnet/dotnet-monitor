// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Tools.Monitor.Swagger.Filters
{
    internal sealed class TooManyRequestsResponseDocumentFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            OpenApiResponse tooManyRequests = new();
            tooManyRequests.Description = "TooManyRequests";

            tooManyRequests.Content ??= new Dictionary<string, OpenApiMediaType>();
            tooManyRequests.Content.Add(
                 ContentTypes.ApplicationProblemJson,
                 new OpenApiMediaType()
                 {
                     Schema = new OpenApiSchemaReference(nameof(ProblemDetails), hostDocument: null)
                 });

            swaggerDoc.Components ??= new OpenApiComponents();
            swaggerDoc.Components.Responses ??= new Dictionary<string, IOpenApiResponse>();
            swaggerDoc.Components.Responses.Add(
                ResponseNames.TooManyRequestsResponse,
                tooManyRequests);
        }
    }
}
