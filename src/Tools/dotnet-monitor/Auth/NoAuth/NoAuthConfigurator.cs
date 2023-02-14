// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;

namespace Microsoft.Diagnostics.Tools.Monitor.Auth.NoAuth
{
    internal sealed class NoAuthConfigurator : IAuthenticationConfigurator
    {
        public void ConfigureApiAuth(IServiceCollection services, HostBuilderContext context)
        {
            services.AddAuthorization(authOptions =>
            {
                authOptions.AddPolicy(AuthConstants.PolicyName, (builder) =>
                {
                    builder.RequireAssertion((_) => true);
                });
            });
        }

        public void ConfigureSwaggerGenAuth(SwaggerGenOptions options)
        {
        }

        public IStartupLogger CreateStartupLogger(ILogger<Startup> logger, IServiceProvider _)
        {
            return new AuthenticationStartupLoggerWrapper(logger.NoAuthentication);
        }
    }
}
