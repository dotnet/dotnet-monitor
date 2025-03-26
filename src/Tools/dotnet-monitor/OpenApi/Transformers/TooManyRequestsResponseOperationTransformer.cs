﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Models.References;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.OpenApi.Transformers
{
    internal sealed class TooManyRequestsResponseOperationTransformer : IOpenApiOperationTransformer
    {
        public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
        {
            var responses = operation.Responses ??= new OpenApiResponses();
            if (responses.Remove(StatusCodeStrings.Status429TooManyRequests))
            {
                responses.Add(
                    StatusCodeStrings.Status429TooManyRequests,
                    new OpenApiResponseReference(ResponseNames.TooManyRequestsResponse));
            }

            return Task.CompletedTask;
        }
    }
}
