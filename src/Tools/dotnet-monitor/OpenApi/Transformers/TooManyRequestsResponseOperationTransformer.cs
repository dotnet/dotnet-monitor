// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.OpenApi.Transformers
{
    internal sealed class TooManyRequestsResponseOperationTransformer : IOpenApiOperationTransformer
    {
        public async Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
        {
            if (operation.Responses.Remove(StatusCodeStrings.Status429TooManyRequests))
            {
                operation.Responses.Add(
                    StatusCodeStrings.Status429TooManyRequests,
                    new OpenApiResponse()
                    {
                        Reference = new OpenApiReference()
                        {
                            Id = ResponseNames.TooManyRequestsResponse,
                            Type = ReferenceType.Response
                        }
                    });
            }
        }
    }
}
