// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Diagnostics.Tools.Monitor.Auth.ApiKey
{
    internal sealed class GeneratedJwtKey
    {
        public readonly string Token;
        public readonly string Subject;
        public readonly string PublicKey;

        private GeneratedJwtKey(string token, string subject, string publicKey)
        {
            Token = token;
            Subject = subject;
            PublicKey = publicKey;
        }

        public static GeneratedJwtKey Create(TimeSpan expirationOffset)
        {
            if (expirationOffset.TotalSeconds <= 0)
            {
                throw new ArgumentException(Strings.ErrorMessage_ExpirationMustBePositive, nameof(expirationOffset));
            }

            Guid subjectId = Guid.NewGuid();
            string subjectStr = subjectId.ToString("D");

            ECDsa dsa = ECDsa.Create();
            dsa.GenerateKey(ECCurve.NamedCurves.nistP384);
            ECDsaSecurityKey secKey = new ECDsaSecurityKey(dsa);
            SigningCredentials signingCreds = new SigningCredentials(secKey, SecurityAlgorithms.EcdsaSha384);
            JwtHeader newHeader = new JwtHeader(signingCreds, null, JwtConstants.HeaderType);

            long expirationSecondsSinceEpoch = EpochTime.GetIntDate(DateTime.UtcNow + expirationOffset);

            Claim audClaim = new Claim(AuthConstants.ClaimAudienceStr, AuthConstants.ApiKeyJwtAudience);
            Claim expClaim = new Claim(AuthConstants.ClaimExpirationStr, expirationSecondsSinceEpoch.ToString());
            Claim issClaim = new Claim(AuthConstants.ClaimIssuerStr, AuthConstants.ApiKeyJwtInternalIssuer);
            Claim subClaim = new Claim(AuthConstants.ClaimSubjectStr, subjectStr);
            JwtPayload newPayload = new JwtPayload(new Claim[] { audClaim, expClaim, issClaim, subClaim });

            JwtSecurityToken newToken = new JwtSecurityToken(newHeader, newPayload);

            ECDsa pubDsa = ECDsa.Create(dsa.ExportParameters(includePrivateParameters: false));
            ECDsaSecurityKey pubSecKey = new ECDsaSecurityKey(pubDsa);
            JsonWebKey jwk = JsonWebKeyConverter.ConvertFromECDsaSecurityKey(pubSecKey);
            string publicKeyJson = JsonSerializer.Serialize(jwk, new JsonSerializerOptions() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
            string publicKeyEncoded = Base64UrlEncoder.Encode(publicKeyJson);

            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
            string output = tokenHandler.WriteToken(newToken);

            return new GeneratedJwtKey(output, subjectStr, publicKeyEncoded);
        }
    }
}
