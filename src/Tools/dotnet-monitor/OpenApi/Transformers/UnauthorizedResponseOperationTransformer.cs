// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Models.References;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.OpenApi.Transformers
{
    /// <summary>
    /// Clears all content of the 401 response and adds a reference to the
    /// UnauthorizedResponse response component <see cref="UnauthorizedResponseDocumentTransformer"/>.
    /// </summary>
    internal sealed class UnauthorizedResponseOperationTransformer : IOpenApiOperationTransformer
    {
        public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
        {
            var responses = operation.Responses ??= new OpenApiResponses();
            if (responses.Remove(StatusCodeStrings.Status401Unauthorized))
            {
                responses.Add(
                    StatusCodeStrings.Status401Unauthorized,
                    new OpenApiResponseReference(ResponseNames.UnauthorizedResponse));
            }

            return Task.CompletedTask;
        }
    }
}
