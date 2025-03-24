// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{

    public class EgressValidationUnhandledExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        public EgressValidationUnhandledExceptionMiddleware(RequestDelegate next, ILogger<EgressValidationUnhandledExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (EgressValidationExtensions.EgressDisabledException egressException)
            {
                var problemDetails = egressException.ToProblemDetails(StatusCodes.Status400BadRequest);
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(problemDetails, options: null, contentType: ContentTypes.ApplicationProblemJson);

                _logger.LogError(egressException.Message);
            }
        }
    }

    public static class EgressValidationExtensions
    {
        private const string EgressQuery = "egressprovider";

        public static RouteHandlerBuilder RequireEgressValidation(this RouteHandlerBuilder builder)
        {
            return builder.AddEndpointFilter(async (context, next) =>
            {
                var services = context.HttpContext.RequestServices; // Resolve per request
                var egressOutputConfiguration = services.GetRequiredService<IEgressOutputConfiguration>();

                StringValues value;
                bool egressProviderGiven = context.HttpContext.Request.Query.TryGetValue(EgressQuery, out value);

                if (!egressProviderGiven || StringValues.IsNullOrEmpty(value))
                {
                    if (!egressOutputConfiguration.IsHttpEgressEnabled)
                    {
                        throw new EgressDisabledException();
                    }
                }
                return await next(context);
            });
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
