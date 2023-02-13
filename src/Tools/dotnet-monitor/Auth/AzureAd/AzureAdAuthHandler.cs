// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Identity.Web;
using System.Collections.Generic;
using System;
using System.Linq;
using Swashbuckle.AspNetCore.SwaggerUI;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.OpenApi.Models;

namespace Microsoft.Diagnostics.Tools.Monitor.Auth.AzureAd
{
    internal sealed class AzureAdAuthHandler : IAuthHandler
    {
        private readonly AzureAdOptions _azureAdOptions;
        private readonly Dictionary<string, string> _scopes;

        public AzureAdAuthHandler(AzureAdOptions azureAdOptions)
        {
            _azureAdOptions = azureAdOptions;

            _scopes = new(1);
            if (_azureAdOptions.RequireScope != null)
            {
                Uri audience = _azureAdOptions.Audience == null ? new Uri($"api://{_azureAdOptions.ClientId}") : new Uri(_azureAdOptions.Audience);
                _scopes.Add(new Uri(audience, _azureAdOptions.RequireScope).ToString(), "Application API Permissions");
            }
        }

        public void ConfigureApiAuth(IServiceCollection services, HostBuilderContext context)
        {
            // Create in-memory representation of our AzureAdOptions so that our defaults applies
            // and we only pass fields supported by our schema.
            Dictionary<string, string> config = new Dictionary<string, string>
            {
                { ConfigurationPath.Combine(ConfigurationKeys.AzureAd, nameof(AzureAdOptions.Instance)), _azureAdOptions.Instance },
                { ConfigurationPath.Combine(ConfigurationKeys.AzureAd, nameof(AzureAdOptions.TenantId)), _azureAdOptions.TenantId },
                { ConfigurationPath.Combine(ConfigurationKeys.AzureAd, nameof(AzureAdOptions.ClientId)), _azureAdOptions.ClientId },
                { ConfigurationPath.Combine(ConfigurationKeys.AzureAd, nameof(AzureAdOptions.Audience)), _azureAdOptions.Audience },
            };

            IConfiguration azureAdConfig = new ConfigurationBuilder().AddInMemoryCollection(config).Build();
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddMicrosoftIdentityWebApi(azureAdConfig);

            List<string> requiredScopes = new(1);
            if (_azureAdOptions.RequireScope != null)
            {
                requiredScopes.Add(_azureAdOptions.RequireScope);
            }

            List<string> requiredRoles = new(1);
            if (_azureAdOptions.RequireRole != null)
            {
                requiredScopes.Add(_azureAdOptions.RequireRole);
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
                        Scopes = _scopes
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
                    _scopes.Keys.ToArray()
                }
            });
        }

        public void ConfigureSwaggerUI(SwaggerUIOptions options)
        {
            options.OAuthUsePkce();
            options.OAuthClientId(_azureAdOptions.ClientId);
            options.OAuthScopes(_scopes.Keys.ToArray());
        }

        public void LogStartup(ILogger logger, IServiceProvider serviceProvider)
        {
        }
    }
}
