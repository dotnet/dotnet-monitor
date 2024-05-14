// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    /// <summary>
    /// We want to restrict the Prometheus scraping endpoint to only the /metrics call.
    /// To do this, we determine what port the request is on, and disallow other actions on the prometheus port.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class HostRestrictionAttribute : Attribute, IActionConstraintFactory
    {
        private sealed class HostConstraint : IActionConstraint
        {
            private readonly int[] _restrictedPorts;

            public HostConstraint(int[] restrictedPorts)
            {
                _restrictedPorts = restrictedPorts;
            }

            public int Order => 0;

            public bool Accept(ActionConstraintContext context)
            {
                return !_restrictedPorts.Any(port => context.RouteContext.HttpContext.Request.Host.Port == port);
            }
        }

        public bool IsReusable => true;

        public IActionConstraint CreateInstance(IServiceProvider services)
        {
            return new HostConstraint(services.GetRequiredService<IMetricsPortsProvider>().MetricsPorts.ToArray());
        }
    }
}
