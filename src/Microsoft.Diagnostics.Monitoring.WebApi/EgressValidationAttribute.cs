// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Logging;
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

    [AttributeUsage(AttributeTargets.Class)]
    public class EgressValidationUnhandledExceptionFilter : ActionFilterAttribute, IExceptionFilter
    {
        private ILogger _logger;

        public EgressValidationUnhandledExceptionFilter(ILogger<EgressValidationUnhandledExceptionFilter> logger)
        {
            _logger = logger;
        }

        public void OnException(ExceptionContext context)
        {
            if (context.Exception is EgressValidationActionFilter.EgressDisabledException egressException)
            {
                BadRequestObjectResult badRequestResult = new BadRequestObjectResult(egressException.Message);
                badRequestResult.Value = ExceptionExtensions.ToProblemDetails(egressException, StatusCodes.Status400BadRequest);

                context.Result = badRequestResult;

                _logger.LogError(Strings.ErrorMessage_HttpEgressDisabled);
            }
        }
    }

    public class EgressValidationActionFilter : IActionFilter
    {
        private IEgressOutputOptions _egressOutputOptions;
        private const string EgressQuery = "egressprovider";

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

            bool egressProviderGiven = context.HttpContext.Request.Query.TryGetValue(EgressQuery, out value);

            if (!egressProviderGiven || StringValues.IsNullOrEmpty(value))
            {
                if (!_egressOutputOptions.IsHttpEgressEnabled)
                {
                    throw new EgressDisabledException();
                }
            }
        }

        public class EgressDisabledException : Exception
        {
            public override string Message
            {
                get
                {
                    return "Http Egress is currently disabled.";
                }
            }
        }
    }
}
