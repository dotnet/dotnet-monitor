// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    /// <summary>
    /// Overrides the behavior of the Produces attribute, which clears all content types from the result and
    /// sets the content types from the attribute to the result. This attribute does the same except that if
    /// the result is a Bad Request, then only add the application/problem+json content type to the result.
    /// </summary>
    /// <remarks>
    /// This alleviates an issue where application/problem+json would never be returned if some other content
    /// type, such as application/json, was also specified. The DefaultOutputFormatterSelector will format the
    /// content using any acceptable content type, which both application/json and application/problem+json are
    /// acceptable to the SystemTextJsonOutputFormatter, thus it chooses the first one: application/json. This
    /// selection is incorrect for error results, which typically want application/problem+json.
    /// 
    /// The intent of this attribute is to make sure that the output is unconditionally reported with a content
    /// type of application/problem+json if the result is a Bad Request.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ProducesWithProblemDetailsAttribute : ProducesAttribute
    {
        public ProducesWithProblemDetailsAttribute(string contentType, params string[] additionalContentTypes)
            : base(contentType, additionalContentTypes)
        {
        }

        public override void OnResultExecuting(ResultExecutingContext context)
        {
            if (context.Result is BadRequestObjectResult badRequestResult && badRequestResult.Value is ProblemDetails)
            {
                badRequestResult.ContentTypes.Add(WebApi.ContentTypes.ApplicationProblemJson);
            }
            else
            {
                base.OnResultExecuting(context);
            }
        }
    }
}
