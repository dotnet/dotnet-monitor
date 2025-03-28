// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Models.Interfaces;
using Microsoft.OpenApi.Models.References;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

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
                        options.TenantId = _azureAdOptions.TenantId;
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

        public void ConfigureOpenApiGenAuth(OpenApiOptions options)
        {
            const string OAuth2SecurityDefinitionName = "OAuth2";
            
            Uri baseEndpoint = new Uri(_azureAdOptions.GetInstance(), FormattableString.Invariant($"{_azureAdOptions.TenantId}/oauth2/v2.0/"));

            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                var components = document.Components ??= new OpenApiComponents();
                var securitySchemes = components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                securitySchemes.Add(OAuth2SecurityDefinitionName, new OpenApiSecurityScheme
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

                var securityRequirements = document.Security ??= new List<OpenApiSecurityRequirement>();
                securityRequirements.Add(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecuritySchemeReference(OAuth2SecurityDefinitionName),
                        Array.Empty<string>()
                    }
                });

                return Task.CompletedTask;
            });
        }

        public IStartupLogger CreateStartupLogger(ILogger<Startup> logger, IServiceProvider _)
        {
            return new AuthenticationStartupLoggerWrapper(() => { });
        }
    }
}
