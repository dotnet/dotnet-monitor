// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.Auth.ApiKey.Stored;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Diagnostics.Tools.Monitor.Auth.ApiKey
{
    internal static class JwtBearerOptionsExtensions
    {
        public static void ConfigureApiKeyTokenValidation(this JwtBearerOptions options, SecurityKey publicKey)
        {
            TokenValidationParameters tokenValidationParameters = new TokenValidationParameters
            {
                // Signing Settings
                RequireSignedTokens = true,
                ValidAlgorithms = JwtAlgorithmChecker.GetAllowedJwsAlgorithmList(),

                // Issuer Settings
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = new SecurityKey[] { publicKey },
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
