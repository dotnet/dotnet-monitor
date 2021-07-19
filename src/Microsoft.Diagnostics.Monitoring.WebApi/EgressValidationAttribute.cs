// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using System;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class EgressValidationAttribute : ActionFilterAttribute, IFilterFactory
    {
        public bool IsReusable => true;
        IFilterMetadata IFilterFactory.CreateInstance(IServiceProvider serviceProvider)
        {
            var egressOutputOptions = serviceProvider.GetRequiredService<IEgressOutputOptions>();

            EgressValidationActionFilter actionFilter = new EgressValidationActionFilter(egressOutputOptions);

            return actionFilter;
        }
    }

    public class EgressValidationUnhandledExceptionFilter : ActionFilterAttribute, IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            BadRequestObjectResult badRequestResult = new BadRequestObjectResult(context.Exception.Message);
            badRequestResult.Value = ExceptionExtensions.ToProblemDetails(context.Exception, StatusCodes.Status400BadRequest);

            context.Result = badRequestResult;
        }
    }

    public class EgressValidationActionFilter : IActionFilter
    {
        private IEgressOutputOptions _egressOutputOptions;

        public EgressValidationActionFilter(IEgressOutputOptions egressOutputOptions)
        {
            _egressOutputOptions = egressOutputOptions;
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // Not sure we need to do anything here...
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            StringValues value;
            bool egressProvider = context.HttpContext.Request.Query.TryGetValue("egressProvider", out value);

            if (egressProvider == false || value.ToString() == null)
            {
                if (_egressOutputOptions.EgressMode == EgressMode.HttpDisabled)
                {
                    throw new ArgumentException("Http Egress is currently disabled.");
                }
            }
        }
    }
}
