// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Diagnostics.Monitoring.OpenApiGen
{
    internal sealed class TooManyRequestsResponseOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
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
