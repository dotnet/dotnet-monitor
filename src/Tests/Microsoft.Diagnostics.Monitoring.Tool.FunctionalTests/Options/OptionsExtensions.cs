// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Diagnostics.Tools.Monitor.Egress.FileSystem;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace Microsoft.Diagnostics.Monitoring.TestCommon.Options
{
    internal static class OptionsExtensions
    {
        public static RootOptions AddFileSystemEgress(this RootOptions options, string name, string outputPath)
        {
            var egressProvider = new FileSystemEgressProviderOptions()
            {
                DirectoryPath = outputPath
            };

            options.Egress = new EgressOptions
            {
                FileSystem = new Dictionary<string, FileSystemEgressProviderOptions>()
                {
                    { name, egressProvider }
                }
            };

            return options;
        }

        public static RootOptions AddGlobalCounter(this RootOptions options, int intervalSeconds)
        {
            options.GlobalCounter = new GlobalCounterOptions
            {
                IntervalSeconds = intervalSeconds
            };

            return options;
        }

        public static RootOptions AddProviderInterval(this RootOptions options, string name, int intervalSeconds)
        {
            Assert.NotNull(options.GlobalCounter);

            options.GlobalCounter.Providers.Add(name, new GlobalProviderOptions { IntervalSeconds = (float)intervalSeconds });

            return options;
        }

        public static CollectionRuleOptions CreateCollectionRule(this RootOptions rootOptions, string name)
        {
            CollectionRuleOptions options = new();
            rootOptions.CollectionRules.Add(name, options);
            return options;
        }

        public static RootOptions EnableInProcessFeatures(this RootOptions options)
        {
            if (null == options.InProcessFeatures)
            {
                options.InProcessFeatures = new Monitoring.Options.InProcessFeaturesOptions();
            }

            options.InProcessFeatures.Enabled = true;

            return options;
        }

        public static RootOptions SetConnectionMode(this RootOptions options, DiagnosticPortConnectionMode connectionMode)
        {
            if (null == options.DiagnosticPort)
            {
                options.DiagnosticPort = new DiagnosticPortOptions();
            }

            options.DiagnosticPort.ConnectionMode = connectionMode;

            return options;
        }

        public static RootOptions SetDefaultSharedPath(this RootOptions options, string directoryPath)
        {
            if (null == options.Storage)
            {
                options.Storage = new StorageOptions();
            }

            options.Storage.DefaultSharedPath = directoryPath;

            return options;
        }

        public static RootOptions SetDumpTempFolder(this RootOptions options, string directoryPath)
        {
            if (null == options.Storage)
            {
                options.Storage = new StorageOptions();
            }

            options.Storage.DumpTempFolder = directoryPath;

            return options;
        }

        /// <summary>
        /// Sets API Key authentication. Use this overload for most operations, unless specifically testing Authentication or Authorization.
        /// </summary>
        public static RootOptions UseApiKey(this RootOptions options, string algorithmName, Guid subject, out string token)
        {
            string subjectStr = subject.ToString("D");
            Claim audClaim = new Claim(AuthConstants.ClaimAudienceStr, AuthConstants.ApiKeyJwtAudience);
            Claim issClaim = new Claim(AuthConstants.ClaimIssuerStr, AuthConstants.ApiKeyJwtInternalIssuer);
            Claim subClaim = new Claim(AuthConstants.ClaimSubjectStr, subjectStr);
            JwtPayload newPayload = new JwtPayload(new Claim[] { audClaim, issClaim, subClaim });

            return options.UseApiKey(algorithmName, subjectStr, newPayload, out token);
        }

        public static RootOptions UseApiKey(this RootOptions options, string algorithmName, string subject, JwtPayload customPayload, out string token)
        {
            return options.UseApiKey(algorithmName, subject, customPayload, out token, out SecurityKey _);
        }

        public static RootOptions UseApiKey(this RootOptions options, string algorithmName, string subject, JwtPayload customPayload, out string token, out SecurityKey privateKey)
        {
            if (null == options.Authentication)
            {
                options.Authentication = new AuthenticationOptions();
            }

            if (null == options.Authentication.MonitorApiKey)
            {
                options.Authentication.MonitorApiKey = new MonitorApiKeyOptions();
            }

            SigningCredentials signingCreds;
            JsonWebKey exportableJwk;
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
                    privateKey = ecSecKey;
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
                    privateKey = rsaSecKey;
                    break;

                case SecurityAlgorithms.HmacSha256:
                case SecurityAlgorithms.HmacSha384:
                case SecurityAlgorithms.HmacSha512:
                    HMAC hmac = GetHmacAlgorithmFromName(algorithmName);
                    SymmetricSecurityKey hmacSecKey = new SymmetricSecurityKey(hmac.Key);
                    signingCreds = new SigningCredentials(hmacSecKey, algorithmName);
                    exportableJwk = JsonWebKeyConverter.ConvertFromSymmetricSecurityKey(hmacSecKey);
                    privateKey = hmacSecKey;
                    break;

                default:
                    throw new ArgumentException($"Algorithm name '{algorithmName}' not supported", nameof(algorithmName));
            }

            JwtHeader newHeader = new JwtHeader(signingCreds, null, JwtConstants.HeaderType);
            JwtSecurityToken newToken = new JwtSecurityToken(newHeader, customPayload);
            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
            string resultToken = tokenHandler.WriteToken(newToken);

            JsonSerializerOptions serializerOptions = JsonSerializerOptionsFactory.Create(JsonIgnoreCondition.WhenWritingNull);
            string publicKeyJson = JsonSerializer.Serialize(exportableJwk, serializerOptions);

            string publicKeyEncoded = Base64UrlEncoder.Encode(publicKeyJson);

            options.Authentication.MonitorApiKey.Subject = subject;
            options.Authentication.MonitorApiKey.PublicKey = publicKeyEncoded;

            token = resultToken;

            return options;
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
