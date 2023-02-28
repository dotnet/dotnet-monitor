// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Tools.Monitor.Swagger.Filters
{
    /// <summary>
    /// Removes failure content types (e.g. application/problem+json) from success operations.
    /// </summary>
    internal sealed class RemoveFailureContentTypesOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            foreach (KeyValuePair<string, OpenApiResponse> response in operation.Responses)
            {
                if (response.Key.StartsWith("2"))
                {
                    response.Value.Content.Remove(ContentTypes.ApplicationProblemJson);
                }
            }
        }
    }
}
