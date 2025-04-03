// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Models.Interfaces;
using Microsoft.OpenApi.Models.References;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.OpenApi.Transformers
{
    internal sealed class TooManyRequestsResponseDocumentTransformer : IOpenApiDocumentTransformer
    {
        public Task TransformAsync(OpenApiDocument openApiDoc, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            OpenApiResponse tooManyRequests = new();
            tooManyRequests.Description = "TooManyRequests";

            tooManyRequests.Content.Add(
                 ContentTypes.ApplicationProblemJson,
                 new OpenApiMediaType()
                 {
                     Schema = new OpenApiSchemaReference(nameof(ProblemDetails))
                 });

            OpenApiComponents components = openApiDoc.Components ??= new OpenApiComponents();
            IDictionary<string, IOpenApiResponse> responses = components.Responses ??= new OpenApiResponses();
            responses.Add(
                ResponseNames.TooManyRequestsResponse,
                tooManyRequests);
            
            return Task.CompletedTask;
        }
    }
}
