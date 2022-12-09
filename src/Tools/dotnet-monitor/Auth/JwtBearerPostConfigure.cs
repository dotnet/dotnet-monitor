// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Linq;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    /// <summary>
    /// Configures <see cref="JwtBearerOptions"/> based on <see cref="MonitorApiKeyConfiguration" /> configuration.
    /// </summary>
    internal sealed class JwtBearerPostConfigure :
        IPostConfigureOptions<JwtBearerOptions>
    {
        private readonly IOptionsMonitor<MonitorApiKeyConfiguration> _apiKeyConfig;

        public JwtBearerPostConfigure(
            IOptionsMonitor<MonitorApiKeyConfiguration> apiKeyConfig)
        {
            _apiKeyConfig = apiKeyConfig;
        }

        public void PostConfigure(string name, JwtBearerOptions options)
        {
            MonitorApiKeyConfiguration configSnapshot = _apiKeyConfig.CurrentValue;
            if (!configSnapshot.Configured || configSnapshot.ValidationErrors.Any())
            {
                options.SecurityTokenValidators.Add(new RejectAllSecurityValidator());
                return;
            }

            TokenValidationParameters tokenValidationParameters = new TokenValidationParameters
            {
                // Signing Settings
                RequireSignedTokens = true,
                ValidAlgorithms = JwtAlgorithmChecker.GetAllowedJwsAlgorithmList(),

                // Issuer Settings
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = new SecurityKey[] { configSnapshot.PublicKey },
                TryAllIssuerSigningKeys = true,

                // Audience Settings
                ValidateAudience = true,
                ValidAudiences = new string[] { AuthConstants.ApiKeyJwtAudience },

                // Other Settings
                ValidateActor = false,
                ValidateLifetime = false,
            };
            options.TokenValidationParameters = tokenValidationParameters;
        }
    }
}
