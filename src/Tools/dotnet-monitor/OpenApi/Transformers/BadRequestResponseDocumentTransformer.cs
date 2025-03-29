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
    /// <summary>
    /// Adds an BadRequestResponse response component to the document.
    /// </summary>
    internal sealed class BadRequestResponseDocumentTransformer : IOpenApiDocumentTransformer
    {
        public Task TransformAsync(OpenApiDocument openApiDoc, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            OpenApiResponse unauthorizedResponse = new();
            unauthorizedResponse.Description = "Bad Request";
            unauthorizedResponse.Content.Add(
                ContentTypes.ApplicationProblemJson,
                new OpenApiMediaType()
                {
                    Schema = new OpenApiSchemaReference(nameof(ValidationProblemDetails))
                });

            OpenApiComponents components = openApiDoc.Components ??= new OpenApiComponents();
            IDictionary<string, IOpenApiResponse> responses = components.Responses ??= new OpenApiResponses();
            responses.Add(
                ResponseNames.BadRequestResponse,
                unauthorizedResponse);

            return Task.CompletedTask;
        }
    }
}
