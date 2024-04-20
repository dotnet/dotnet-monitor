// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Microsoft.Diagnostics.Monitoring.TestCommon.Options
{
    internal static partial class OptionsExtensions
    {
        /// <summary>
        /// Sets API Key authentication. Use this overload for most operations, unless specifically testing Authentication
        /// </summary>
        public static RootOptions UseApiKey(this RootOptions options, string algorithmName, Guid subject, out string token)
        {
            string subjectStr = subject.ToString("D");
            Claim audClaim = new Claim(AuthConstants.ClaimAudienceStr, AuthConstants.ApiKeyJwtAudience);
            long expirationSecondsSinceEpoch = EpochTime.GetIntDate(DateTime.UtcNow + AuthConstants.ApiKeyJwtDefaultExpiration);
            Claim expClaim = new Claim(AuthConstants.ClaimExpirationStr, expirationSecondsSinceEpoch.ToString(CultureInfo.InvariantCulture));
            Claim issClaim = new Claim(AuthConstants.ClaimIssuerStr, AuthConstants.ApiKeyJwtInternalIssuer);
            Claim subClaim = new Claim(AuthConstants.ClaimSubjectStr, subjectStr);
            JwtPayload newPayload = new JwtPayload(new Claim[] { audClaim, expClaim, issClaim, subClaim });

            return options.UseApiKey(algorithmName, subjectStr, newPayload, out token);
        }

        public static RootOptions UseApiKey(this RootOptions options, string algorithmName, string subject, JwtPayload customPayload, out string token)
        {
            return options.UseApiKey(ApiKeySignInfo.Create(algorithmName), subject, customPayload, out token);
        }

        public static RootOptions UseApiKey(this RootOptions options, ApiKeySignInfo signInfo, string subject, JwtPayload customPayload, out string token)
        {
            options.UseApiKey(signInfo, subject);

            token = ApiKeyToken.Create(signInfo, customPayload);

            return options;
        }

        public static RootOptions UseApiKey(this RootOptions options, ApiKeySignInfo signInfo, string subject, string issuer = null)
        {
            if (null == options.Authentication)
            {
                options.Authentication = new AuthenticationOptions();
            }

            if (null == options.Authentication.MonitorApiKey)
            {
                options.Authentication.MonitorApiKey = new MonitorApiKeyOptions();
            }

            options.Authentication.MonitorApiKey.Subject = subject;
            options.Authentication.MonitorApiKey.PublicKey = signInfo.PublicKeyEncoded;
            if (!string.IsNullOrEmpty(issuer))
            {
                options.Authentication.MonitorApiKey.Issuer = issuer;
            }

            return options;
        }
    }
}
