// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;
using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Tools.Monitor.Auth.ApiKey
{
    internal abstract class AbstractMonitorKeyAuthHandler : IAuthHandler
    {
        private readonly bool _enableNegotiation;

        public AbstractMonitorKeyAuthHandler()
        {
            if (OperatingSystem.IsWindows())
            {
                _enableNegotiation = true;
            }
        }

        public void ConfigureApiAuth(IServiceCollection services, HostBuilderContext context)
        {
            AuthenticationBuilder builder = services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme);

            List<string> authSchemas = new List<string> { AuthConstants.ApiKeySchema };
            if (_enableNegotiation)
            {
                // On Windows add Negotiate package. This will use NTLM to perform Windows Authentication.
                builder.AddNegotiate();
                authSchemas.Add(AuthConstants.NegotiateSchema);
            }

            services.AddAuthorization(authOptions =>
            {
                authOptions.AddPolicy(AuthConstants.PolicyName, (builder) =>
                {
                    // Apply Authorization Policy for NTLM. Without Authorization, any user with a valid login/password will be authorized. We only
                    // want to authorize the same user that is running dotnet-monitor, at least for now.
                    // Note this policy applies to both Authorization schemas.
                    builder.AddRequirements(new AuthorizedUserRequirement());
                    builder.RequireAuthenticatedUser();
                    builder.AddAuthenticationSchemes(authSchemas.ToArray());
                });
            });

            ConfigureAuthBuilder(services, context, builder);
        }

        public void ConfigureSwaggerGenAuth(SwaggerGenOptions options)
        {
            const string ApiKeySecurityDefinitionName = "ApiKeyAuth";

            options.AddSecurityDefinition(ApiKeySecurityDefinitionName, new OpenApiSecurityScheme
            {
                Name = HeaderNames.Authorization,
                Type = SecuritySchemeType.ApiKey,
                Scheme = JwtBearerDefaults.AuthenticationScheme,
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = Strings.HelpDescription_SecurityDefinitionDescription_ApiKey
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = ApiKeySecurityDefinitionName }
                    },
                    Array.Empty<string>()
                }
            });
        }

        protected void LogIfNegotiateIsDisabledDueToElevation(ILogger logger)
        {
            if (_enableNegotiation && EnvironmentInformation.IsElevated)
            {
                logger.DisabledNegotiateWhileElevated();
            }
        }

        protected abstract void ConfigureAuthBuilder(IServiceCollection services, HostBuilderContext context, AuthenticationBuilder authBuilder);

        public abstract void ConfigureSwaggerUI(SwaggerUIOptions options);

        public abstract void LogStartup(ILogger logger, IServiceProvider serviceProvider);
    }
}
