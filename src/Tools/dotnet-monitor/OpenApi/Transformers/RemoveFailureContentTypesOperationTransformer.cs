// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.OpenApi;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Models.Interfaces;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor.OpenApi.Transformers
{
    /// <summary>
    /// Removes failure content types (e.g. application/problem+json) from success operations.
    /// </summary>
    internal sealed class RemoveFailureContentTypesOperationTransformer : IOpenApiOperationTransformer
    {
        public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
        {
            if (null != operation.Responses)
            {
                foreach (KeyValuePair<string, IOpenApiResponse> response in operation.Responses)
                {
                    if (response.Key.StartsWith("2"))
                    {
                        response.Value.Content.Remove(ContentTypes.ApplicationProblemJson);
                    }
                }
            }
            return Task.CompletedTask;
        }
    }
}
