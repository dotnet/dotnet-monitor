// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
            var egressOutputConfiguration = serviceProvider.GetRequiredService<IEgressOutputConfiguration>();

            EgressValidationActionFilter actionFilter = new EgressValidationActionFilter(egressOutputConfiguration);

            return actionFilter;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    internal class EgressValidationUnhandledExceptionFilter : ActionFilterAttribute, IExceptionFilter
    {
        private readonly ILogger _logger;

        public EgressValidationUnhandledExceptionFilter(ILogger<EgressValidationUnhandledExceptionFilter> logger)
        {
            _logger = logger;
        }

        public void OnException(ExceptionContext context)
        {
            if (context.Exception is EgressValidationActionFilter.EgressDisabledException egressException)
            {
                context.Result = new BadRequestObjectResult(egressException.ToProblemDetails(StatusCodes.Status400BadRequest));

                _logger.LogError(egressException.Message);
            }
        }
    }

    internal class EgressValidationActionFilter : IActionFilter
    {
        private readonly IEgressOutputConfiguration _egressOutputConfiguration;
        private const string EgressQuery = "egressprovider";

        public EgressValidationActionFilter(IEgressOutputConfiguration egressOutputConfiguration)
        {
            _egressOutputConfiguration = egressOutputConfiguration;
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            StringValues value;

            bool egressProviderGiven = context.HttpContext.Request.Query.TryGetValue(EgressQuery, out value);

            if (!egressProviderGiven || StringValues.IsNullOrEmpty(value))
            {
                if (!_egressOutputConfiguration.IsHttpEgressEnabled)
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
                    return Strings.ErrorMessage_HttpEgressDisabled;
                }
            }
        }
    }
}
