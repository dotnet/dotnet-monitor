// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.Swagger;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;
using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Tools.Monitor.Auth.AzureAd
{
    internal sealed class AzureAdAuthConfigurator : IAuthenticationConfigurator
    {
        private readonly AzureAdOptions _azureAdOptions;
        private readonly string _fqSwaggerScope;

        public AzureAdAuthConfigurator(AzureAdOptions azureAdOptions)
        {
            _azureAdOptions = azureAdOptions;

            if (_azureAdOptions.SwaggerScope != null)
            {
                _fqSwaggerScope = new Uri(_azureAdOptions.GetAppIdUri(), _azureAdOptions.SwaggerScope).ToString();
            }
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
            const string AzureAdBearerTokenSecurityDefinitionName = "AzureAd JWT";

            // Only present an option to interactively authenticate if a swagger scope is set.
            if (_fqSwaggerScope != null)
            {
                Uri baseEndpoint = new Uri(_azureAdOptions.GetInstance(), $"{_azureAdOptions.GetTenantId()}/oauth2/v2.0/");

                options.AddSecurityDefinition(OAuth2SecurityDefinitionName, new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.OAuth2,
                    Flows = new OpenApiOAuthFlows
                    {
                        AuthorizationCode = new OpenApiOAuthFlow
                        {
                            AuthorizationUrl = new Uri(baseEndpoint, "authorize"),
                            TokenUrl = new Uri(baseEndpoint, "token"),
                            Scopes = new Dictionary<string, string>()
                            {
                                { _fqSwaggerScope, Strings.HelpDescription_SwaggerScope_AzureAd }
                            }
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
                        new [] { _fqSwaggerScope }
                    }
                });
            }

            options.AddBearerTokenAuthOption(AzureAdBearerTokenSecurityDefinitionName);
        }

        public void ConfigureSwaggerUI(SwaggerUIOptions options)
        {
            if (_fqSwaggerScope == null)
            {
                return;
            }

            // Use authorization code flow instead of the implicit flow.
            // AzureAD advises against using implicit flow and requires manual editing of the
            // App Registration manifest to enable.
            options.OAuthUsePkce();

            // Set default field values in the UI.
            options.OAuthClientId(_azureAdOptions.ClientId);
            options.OAuthScopes(_fqSwaggerScope);
        }

        public IStartupLogger CreateStartupLogger(ILogger<Startup> logger, IServiceProvider _)
        {
            return new AuthenticationStartupLoggerWrapper(() => { });
        }
    }
}
