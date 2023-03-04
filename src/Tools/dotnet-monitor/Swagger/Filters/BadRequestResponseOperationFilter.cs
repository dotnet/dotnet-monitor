// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Microsoft.Diagnostics.Tools.Monitor.Swagger.Filters
{
    /// <summary>
    /// Clears all content of the 400 response and adds a reference to the
    /// BadRequestResponse response component <see cref="BadRequestResponseDocumentFilter"/>.
    /// </summary>
    internal sealed class BadRequestResponseOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
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
        }
    }
}
