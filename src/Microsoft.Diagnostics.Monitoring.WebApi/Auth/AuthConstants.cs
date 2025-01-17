// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.Monitoring.WebApi
{
    public static class AuthConstants
    {
        public const string PolicyName = "AuthorizedUserPolicy";
        public const string NegotiateSchema = "Negotiate";
        public const string NtlmSchema = "NTLM";
        public const string KerberosSchema = "Kerberos";
        public const string FederationAuthType = "AuthenticationTypes.Federation";
        public const string ApiKeySchema = "Bearer";
        public const string ApiKeyJwtType = "JWT";
        public const string ApiKeyJwtInternalIssuer = "https://github.com/dotnet/dotnet-monitor/generatekey+MonitorApiKey";
        public const string ApiKeyJwtAudience = "https://github.com/dotnet/dotnet-monitor";
        public const string ClaimAudienceStr = "aud";
        public const string ClaimExpirationStr = "exp";
        public const string ClaimIssuerStr = "iss";
        public const string ClaimSubjectStr = "sub";

        public static readonly TimeSpan ApiKeyJwtDefaultExpiration = TimeSpan.FromDays(7);
    }
}
