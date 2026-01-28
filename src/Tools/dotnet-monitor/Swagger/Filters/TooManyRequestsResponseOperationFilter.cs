// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Microsoft.Diagnostics.Tools.Monitor.Swagger.Filters
{
    internal sealed class TooManyRequestsResponseOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.Responses?.Remove(StatusCodeStrings.Status429TooManyRequests) == true)
            {
                operation.Responses.Add(
                    StatusCodeStrings.Status429TooManyRequests,
                    new OpenApiResponseReference(ResponseNames.TooManyRequestsResponse, hostDocument: null));
            }
        }
    }
}
