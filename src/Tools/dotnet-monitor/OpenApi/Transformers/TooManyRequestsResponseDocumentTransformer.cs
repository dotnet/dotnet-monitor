// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.OpenApi.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.OpenApi.Transformers
{
    internal sealed class TooManyRequestsResponseDocumentTransformer : IOpenApiDocumentTransformer
    {
        public async Task TransformAsync(OpenApiDocument swaggerDoc, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            OpenApiResponse tooManyRequests = new();
            tooManyRequests.Description = "TooManyRequests";

            tooManyRequests.Content.Add(
                 ContentTypes.ApplicationProblemJson,
                 new OpenApiMediaType()
                 {
                     Schema = new OpenApiSchema()
                     {
                         Reference = new OpenApiReference()
                         {
                             Id = nameof(ProblemDetails),
                             Type = ReferenceType.Schema
                         }
                     }
                 });

            swaggerDoc.Components.Responses.Add(
                ResponseNames.TooManyRequestsResponse,
                tooManyRequests);
        }
    }
}
