// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;

namespace Microsoft.Diagnostics.Tools.Monitor.Auth.AzureAd
{
    internal sealed class AzureAdAuthConfigurator : IAuthenticationConfigurator
    {
        private readonly AzureAdOptions _azureAdOptions;

        public AzureAdAuthConfigurator(AzureAdOptions azureAdOptions)
        {
            _azureAdOptions = azureAdOptions;
        }

        public void ConfigureApiAuth(IServiceCollection services, HostBuilderContext context)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(
                    configureJwtBearerOptions: options =>
                    {
                        options.Audience = _azureAdOptions.GetAppIdUri().ToString();
                    },
                    configureMicrosoftIdentityOptions: options =>
                    {
                        options.Instance = _azureAdOptions.GetInstance().ToString();
                        options.TenantId = _azureAdOptions.GetTenantId();
                        options.ClientId = _azureAdOptions.ClientId;
                    }
                );

            services.AddAuthorization(options =>
            {
                options.AddPolicy(AuthConstants.PolicyName, (builder) =>
                {
                    builder.RequireRole(_azureAdOptions.RequiredRole);
                });
            });
        }

        public void ConfigureSwaggerGenAuth(SwaggerGenOptions options)
        {
            const string OAuth2SecurityDefinitionName = "OAuth2";

            Uri baseEndpoint = new Uri(_azureAdOptions.GetInstance(), FormattableString.Invariant($"{_azureAdOptions.GetTenantId()}/oauth2/v2.0/"));

            options.AddSecurityDefinition(OAuth2SecurityDefinitionName, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows
                {
                    AuthorizationCode = new OpenApiOAuthFlow
                    {
                        AuthorizationUrl = new Uri(baseEndpoint, "authorize"),
                        TokenUrl = new Uri(baseEndpoint, "token")
                    }
                }
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type= ReferenceType.SecurityScheme, Id = OAuth2SecurityDefinitionName }
                    },
                    Array.Empty<string>()
                }
            });
        }

        public IStartupLogger CreateStartupLogger(ILogger<Startup> logger, IServiceProvider _)
        {
            return new AuthenticationStartupLoggerWrapper(() => { });
        }
    }
}
