﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.Auth.ApiKey.Temporary;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Tools.Monitor.Auth.ApiKey
{
    internal sealed class MonitorKeyAuthConfigurator : IAuthenticationConfigurator
    {
        private readonly GeneratedJwtKey _pinnedJwtKey;
        private readonly bool _enableNegotiation;


        public MonitorKeyAuthConfigurator(GeneratedJwtKey pinnedJwtKey = null)
        {
            _pinnedJwtKey = pinnedJwtKey;

            if (OperatingSystem.IsWindows())
            {
                _enableNegotiation = false;
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

            services.ConfigureMonitorApiKeyAuthentication(context.Configuration, builder, allowConfigurationUpdates: _pinnedJwtKey == null);
            services.AddSingleton<IAuthorizationHandler, UserAuthorizationHandler>();
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

        public IStartupLogger CreateStartupLogger(ILogger<Startup> logger, IServiceProvider serviceProvider)
        {
            return new AuthenticationStartupLoggerWrapper(() =>
            {
                if (_enableNegotiation && EnvironmentInformation.IsElevated)
                {
                    logger.DisabledNegotiateWhileElevated();
                }

                if (_pinnedJwtKey != null)
                {
                    logger.LogTempKey(_pinnedJwtKey.Token);
                }
                else
                {
                    MonitorApiKeyConfigurationObserver observer = serviceProvider.GetRequiredService<MonitorApiKeyConfigurationObserver>();
                    observer.Initialize();
                }
            });
        }
    }
}
