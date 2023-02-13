// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Swashbuckle.AspNetCore.SwaggerUI;
using Microsoft.Extensions.Logging;
using System;

namespace Microsoft.Diagnostics.Tools.Monitor.Auth.NoAuth
{
    internal sealed class NoAuthHandler : IAuthHandler
    {
        public void ConfigureApiAuth(IServiceCollection services, HostBuilderContext context)
        {
            services.AddAuthorization(authOptions =>
            {
                authOptions.AddPolicy(AuthConstants.PolicyName, (builder) =>
                {
                    builder.RequireAssertion((f) => true);
                });
            });
        }

        public void ConfigureSwaggerGenAuth(SwaggerGenOptions options)
        {
        }

        public void ConfigureSwaggerUI(SwaggerUIOptions options)
        {

        }

        public void LogStartup(ILogger logger, IServiceProvider serviceProvider)
        {
            logger.NoAuthentication();
        }
    }
}
