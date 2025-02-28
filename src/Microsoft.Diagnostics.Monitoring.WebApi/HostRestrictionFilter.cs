// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    public class HostRestrictionFilter : IEndpointFilter
    {
        private readonly int[] _restrictedPorts;

        public HostRestrictionFilter(int[] restrictedPorts)
        {
            _restrictedPorts = restrictedPorts;
        }

        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            var httpContext = context.HttpContext;
            var requestPort = httpContext.Request.Host.Port;

            if (requestPort.HasValue && _restrictedPorts.Contains(requestPort.Value))
            {
                return Results.Forbid();
            }

            return await next(context);
        }
    }

    public static class HostRestrictionExtensions
    {
        public static RouteHandlerBuilder RequireHostRestriction(this RouteHandlerBuilder builder)
        {
            return builder.AddEndpointFilter(async (context, next) =>
            {
                var services = context.HttpContext.RequestServices; // Resolve per request
                var metricsPortsProvider = services.GetRequiredService<IMetricsPortsProvider>();
                var restrictedPorts = metricsPortsProvider.MetricsPorts.ToArray(); // Fresh instance per request

                var requestPort = context.HttpContext.Request.Host.Port;
                if (requestPort.HasValue && restrictedPorts.Contains(requestPort.Value))
                {
                    return Results.Forbid(); // 403 Forbidden - Access restricted
                }

                return await next(context);
            });
        }
    }
}
