// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.OpenApi.Transformers
{
    /// <summary>
    /// Clears all content of the 401 response and adds a reference to the
    /// UnauthorizedResponse response component <see cref="UnauthorizedResponseDocumentFilter"/>.
    /// </summary>
    internal sealed class UnauthorizedResponseOperationTransformer : IOpenApiOperationTransformer
    {
        public async Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
        {
            if (operation.Responses.TryGetValue(StatusCodeStrings.Status401Unauthorized, out OpenApiResponse? unauthorizedResponse))
            {
                unauthorizedResponse.Content.Clear();
                unauthorizedResponse.Reference = new OpenApiReference()
                {
                    Id = ResponseNames.UnauthorizedResponse,
                    Type = ReferenceType.Response
                };
            }
        }
    }
}
