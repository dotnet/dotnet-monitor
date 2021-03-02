using Microsoft.Diagnostics.Monitoring.RestServer;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Diagnostics.Monitoring.OpenApiGen
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
