// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Diagnostics.Monitoring.OpenApiGen
{
    internal sealed class TooManyRequestsResponseDocumentFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            OpenApiResponse tooManyRequests = new();
            tooManyRequests.Description = "TooManyRequests";

            tooManyRequests.Content.Add(
                 ContentTypes.ApplicationProblemJson,
                 new OpenApiMediaType()
                 {
                     Schema = new OpenApiSchema()
                     {
                         Reference = new OpenApiReference()
                         {
                             Id = typeof(ProblemDetails).Name,
                             Type = ReferenceType.Schema
                         }
                     }
                 });

            swaggerDoc.Components.Responses.Add(
                ResponseNames.TooManyRequestsResponse,
                tooManyRequests);
        }
    }
}
