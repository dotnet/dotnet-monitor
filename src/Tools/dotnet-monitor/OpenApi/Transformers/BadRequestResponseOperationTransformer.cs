﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.OpenApi.Transformers
{
    /// <summary>
    /// Clears all content of the 400 response and adds a reference to the
    /// BadRequestResponse response component <see cref="BadRequestResponseDocumentTransformer"/>.
    /// </summary>
    internal sealed class BadRequestResponseOperationTransformer : IOpenApiOperationTransformer
    {
        public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
        {
            if (operation.Responses.Remove(StatusCodeStrings.Status400BadRequest))
            {
                operation.Responses.Add(
                    StatusCodeStrings.Status400BadRequest,
                    new OpenApiResponse()
                    {
                        Reference = new OpenApiReference()
                        {
                            Id = ResponseNames.BadRequestResponse,
                            Type = ReferenceType.Response
                        }
                    });
            }

            return Task.CompletedTask;
        }
    }
}
