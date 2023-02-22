// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Identity.Web;
using System.Collections.Generic;
using System;
using Swashbuckle.AspNetCore.SwaggerUI;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.OpenApi.Models;

namespace Microsoft.Diagnostics.Tools.Monitor.Auth.AzureAd
{
    internal sealed class AzureAdAuthConfigurator : IAuthenticationConfigurator
    {
        private readonly AzureAdOptions _azureAdOptions;
        private readonly string _appIdUri;
        private readonly string _fqRequiredScope;

        public AzureAdAuthConfigurator(AzureAdOptions azureAdOptions)
        {
            _azureAdOptions = azureAdOptions;
            _appIdUri = _azureAdOptions.AppIdUri ?? $"api://{_azureAdOptions.ClientId}";

            if (_azureAdOptions.RequiredScope != null)
            {
                _fqRequiredScope = $"{_appIdUri}/{_azureAdOptions.RequiredScope}";
            }
        }

        public void ConfigureApiAuth(IServiceCollection services, HostBuilderContext context)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(
                    configureJwtBearerOptions: options =>
                    {
                        options.Audience = _appIdUri;
                    },
                    configureMicrosoftIdentityOptions: options =>
                    {
                        options.Instance = _azureAdOptions.Instance;
                        options.TenantId = _azureAdOptions.TenantId;
                        options.ClientId = _azureAdOptions.ClientId;
                    }
                );

            List<string> requiredScopes = new(1);
            if (_azureAdOptions.RequiredScope != null)
            {
                requiredScopes.Add(_azureAdOptions.RequiredScope);
            }

            List<string> requiredRoles = new(1);
            if (_azureAdOptions.RequiredRole != null)
            {
                requiredRoles.Add(_azureAdOptions.RequiredRole);
            }

            services.AddAuthorization(options =>
            {
                options.AddPolicy(AuthConstants.PolicyName, (builder) =>
                {
                    builder.RequireScopeOrAppPermission(requiredScopes, requiredRoles);
                });
            });
        }

        public void ConfigureSwaggerGenAuth(SwaggerGenOptions options)
        {
            const string OAuth2SecurityDefinitionName = "OAuth2";

            // Only present an option to authenticate if a required scope is set.
            // Otherwise a user cannot authenticate, only other applications can.
            if (_fqRequiredScope != null)
            {
                Uri baseEndpoint = new Uri(new Uri(_azureAdOptions.Instance), $"{_azureAdOptions.TenantId}/oauth2/v2.0/");

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
                                { _fqRequiredScope, Strings.HelpDescription_RequiredScope_AzureAd }
                            }
                        }
                    }
                });
            }

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type= ReferenceType.SecurityScheme, Id = OAuth2SecurityDefinitionName }
                    },
                    (_fqRequiredScope != null) ? new [] { _fqRequiredScope } : null
                }
            });
        }

        public void ConfigureSwaggerUI(SwaggerUIOptions options)
        {
            if (_fqRequiredScope == null)
            {
                return;
            }

            // Use authorization code flow instead of the implicit flow.
            // AzureAD advices against using implicit flow and requires manual editing of the
            // App Registration manifest to enable.
            options.OAuthUsePkce();

            options.OAuthClientId(_azureAdOptions.ClientId);
            options.OAuthScopes(_fqRequiredScope);
        }

        public IStartupLogger CreateStartupLogger(ILogger<Startup> logger, IServiceProvider _)
        {
            return new AuthenticationStartupLoggerWrapper(() => { });
        }
    }
}
