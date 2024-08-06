// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Microsoft.Diagnostics.Tools.Monitor.Swagger.Filters
{
    /// <summary>
    /// Clears all content of the 401 response and adds a reference to the
    /// UnauthorizedResponse response component <see cref="UnauthorizedResponseDocumentFilter"/>.
    /// </summary>
    internal sealed class UnauthorizedResponseOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
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
