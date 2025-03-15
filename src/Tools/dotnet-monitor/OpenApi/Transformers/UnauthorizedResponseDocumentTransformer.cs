// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.OpenApi.Transformers
{
    /// <summary>
    /// Adds an UnauthorizedResponse response component to the document.
    /// </summary>
    internal sealed class UnauthorizedResponseDocumentTransformer : IOpenApiDocumentTransformer
    {
        public Task TransformAsync(OpenApiDocument openApiDoc, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            OpenApiHeader authenticateHeader = new();
            authenticateHeader.Schema = new OpenApiSchema() { Type = "string" };

            OpenApiResponse unauthorizedResponse = new();
            unauthorizedResponse.Description = "Unauthorized";
            unauthorizedResponse.Headers.Add("WWW_Authenticate", authenticateHeader);

            openApiDoc.Components.Responses.Add(
                ResponseNames.UnauthorizedResponse,
                unauthorizedResponse);

            return Task.CompletedTask;
        }
    }
}
