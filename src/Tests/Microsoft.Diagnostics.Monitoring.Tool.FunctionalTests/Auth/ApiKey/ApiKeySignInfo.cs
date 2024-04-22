// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.TestCommon;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Diagnostics.Monitoring.Tool.FunctionalTests
{
    internal sealed class ApiKeySignInfo
    {
        public readonly JwtHeader Header;
        public readonly string PublicKeyEncoded;
        public readonly string PrivateKeyEncoded;

        private ApiKeySignInfo(JwtHeader header, string publicKeyEncoded, string privateKeyEncoded)
        {
            Header = header;
            PublicKeyEncoded = publicKeyEncoded;
            PrivateKeyEncoded = privateKeyEncoded;
        }

        public static ApiKeySignInfo Create(string algorithmName)
        {
            SigningCredentials signingCreds;
            JsonWebKey exportableJwk;
            JsonWebKey privateJwk;
            switch (algorithmName)
            {
                case SecurityAlgorithms.EcdsaSha256:
                case SecurityAlgorithms.EcdsaSha256Signature:
                case SecurityAlgorithms.EcdsaSha384:
                case SecurityAlgorithms.EcdsaSha384Signature:
                case SecurityAlgorithms.EcdsaSha512:
                case SecurityAlgorithms.EcdsaSha512Signature:
                    ECDsa ecDsa = ECDsa.Create(GetEcCurveFromName(algorithmName));
                    ECDsaSecurityKey ecSecKey = new ECDsaSecurityKey(ecDsa);
                    signingCreds = new SigningCredentials(ecSecKey, algorithmName);
                    ECDsa pubEcDsa = ECDsa.Create(ecDsa.ExportParameters(false));
                    ECDsaSecurityKey pubEcSecKey = new ECDsaSecurityKey(pubEcDsa);
                    exportableJwk = JsonWebKeyConverter.ConvertFromECDsaSecurityKey(pubEcSecKey);
                    privateJwk = JsonWebKeyConverter.ConvertFromECDsaSecurityKey(ecSecKey);
                    break;

                case SecurityAlgorithms.RsaSha256:
                case SecurityAlgorithms.RsaSha256Signature:
                case SecurityAlgorithms.RsaSha384:
                case SecurityAlgorithms.RsaSha384Signature:
                case SecurityAlgorithms.RsaSha512:
                case SecurityAlgorithms.RsaSha512Signature:
                    RSA rsa = RSA.Create(GetRsaKeyLengthFromName(algorithmName));
                    RsaSecurityKey rsaSecKey = new RsaSecurityKey(rsa);
                    signingCreds = new SigningCredentials(rsaSecKey, algorithmName);
                    RSA pubRsa = RSA.Create(rsa.ExportParameters(false)); // lgtm[cs/weak-asymmetric-algorithm] Intentional testing rejection of weak algorithm
                    RsaSecurityKey pubRsaSecKey = new RsaSecurityKey(pubRsa);
                    exportableJwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(pubRsaSecKey);
                    privateJwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(rsaSecKey);
                    break;

                case SecurityAlgorithms.HmacSha256:
                case SecurityAlgorithms.HmacSha384:
                case SecurityAlgorithms.HmacSha512:
                    HMAC hmac = GetHmacAlgorithmFromName(algorithmName);
                    SymmetricSecurityKey hmacSecKey = new SymmetricSecurityKey(hmac.Key);
                    signingCreds = new SigningCredentials(hmacSecKey, algorithmName);
                    exportableJwk = JsonWebKeyConverter.ConvertFromSymmetricSecurityKey(hmacSecKey);
                    privateJwk = JsonWebKeyConverter.ConvertFromSymmetricSecurityKey(hmacSecKey);
                    break;

                default:
                    throw new ArgumentException($"Algorithm name '{algorithmName}' not supported", nameof(algorithmName));
            }

            JsonSerializerOptions serializerOptions = JsonSerializerOptionsFactory.Create(JsonIgnoreCondition.WhenWritingNull);

            string publicKeyJson = JsonSerializer.Serialize(exportableJwk, serializerOptions);
            string publicKeyEncoded = Base64UrlEncoder.Encode(publicKeyJson);

            string privateKeyJson = JsonSerializer.Serialize(privateJwk, serializerOptions);
            string privateKeyEncoded = Base64UrlEncoder.Encode(privateKeyJson);

            JwtHeader newHeader = new JwtHeader(signingCreds, null, JwtConstants.HeaderType);

            return new ApiKeySignInfo(newHeader, publicKeyEncoded, privateKeyEncoded);
        }

        private static HMAC GetHmacAlgorithmFromName(string algorithmName)
        {
            switch (algorithmName)
            {
                case SecurityAlgorithms.HmacSha256:
                    return new HMACSHA256();
                case SecurityAlgorithms.HmacSha384:
                    return new HMACSHA384();
                case SecurityAlgorithms.HmacSha512:
                    return new HMACSHA512();
                default:
                    throw new ArgumentException($"Algorithm name '{algorithmName}' not supported", nameof(algorithmName));
            }
        }

        private static int GetRsaKeyLengthFromName(string algorithmName)
        {
            switch (algorithmName)
            {
                case SecurityAlgorithms.RsaSha256:
                case SecurityAlgorithms.RsaSha256Signature:
                    return 2048;
                case SecurityAlgorithms.RsaSha384:
                case SecurityAlgorithms.RsaSha384Signature:
                    return 3072;
                case SecurityAlgorithms.RsaSha512:
                case SecurityAlgorithms.RsaSha512Signature:
                    return 4096;
                default:
                    throw new ArgumentException($"Algorithm name '{algorithmName}' not supported", nameof(algorithmName));
            }
        }

        private static ECCurve GetEcCurveFromName(string algorithmName)
        {
            switch (algorithmName)
            {
                case SecurityAlgorithms.EcdsaSha256:
                case SecurityAlgorithms.EcdsaSha256Signature:
                    return ECCurve.NamedCurves.nistP256;
                case SecurityAlgorithms.EcdsaSha384:
                case SecurityAlgorithms.EcdsaSha384Signature:
                    return ECCurve.NamedCurves.nistP384;
                case SecurityAlgorithms.EcdsaSha512:
                case SecurityAlgorithms.EcdsaSha512Signature:
                    return ECCurve.NamedCurves.nistP521;
                default:
                    throw new ArgumentException($"Algorithm name '{algorithmName}' not supported", nameof(algorithmName));
            }
        }
    }
}
