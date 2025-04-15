// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    public class EgressValidationFilter : IEndpointFilter
    {
        private const string EgressQuery = "egressprovider";
        private readonly ILogger _logger;

        public EgressValidationFilter(ILogger<EgressValidationFilter> logger)
        {
            _logger = logger;
        }

        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            var services = context.HttpContext.RequestServices;
            var egressOutputConfiguration = services.GetRequiredService<IEgressOutputConfiguration>();

            StringValues value;
            bool egressProviderGiven = context.HttpContext.Request.Query.TryGetValue(EgressQuery, out value);

            if (!egressProviderGiven || StringValues.IsNullOrEmpty(value))
            {
                if (!egressOutputConfiguration.IsHttpEgressEnabled)
                {
                    var problemDetails = new ProblemDetails
                    {
                        Detail = Strings.ErrorMessage_HttpEgressDisabled,
                        Status = StatusCodes.Status400BadRequest
                    };

                    _logger.LogError(Strings.ErrorMessage_HttpEgressDisabled);

                    return TypedResults.Problem(problemDetails);
                }
            }
            return await next(context);
        }
    }

    public static class EgressValidationExtensions
    {
        public static RouteHandlerBuilder RequireEgressValidation(this RouteHandlerBuilder builder)
        {
            return builder.AddEndpointFilter<EgressValidationFilter>();
        }
    }
}
