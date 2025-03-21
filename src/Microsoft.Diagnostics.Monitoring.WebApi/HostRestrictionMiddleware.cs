// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    public static class HostRestrictionExtensions
    {
        public static RouteHandlerBuilder RequireHostRestriction(this RouteHandlerBuilder builder)
        {
            return builder.WithMetadata(new HostRestrictionAttribute());
        }
    }

    internal class HostRestrictionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMetricsPortsProvider _metricsPortsProvider;

        public HostRestrictionMiddleware(RequestDelegate next, IMetricsPortsProvider metricsPortsProvider)
        {
            _next = next;
            _metricsPortsProvider = metricsPortsProvider;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var metadata = context.GetEndpoint()?.Metadata.GetMetadata<HostRestrictionAttribute>();
            if (metadata != null)
            {
                Console.WriteLine("type? " + metadata.GetType());
                var _restrictedPorts = _metricsPortsProvider.MetricsPorts.ToArray();
                if (_restrictedPorts.Any(port => context.Request.Host.Port == port))
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    return;
                }
            }

            await _next(context);
        }
    }
}
