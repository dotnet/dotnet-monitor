// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;

#if UNITTEST
namespace Microsoft.Diagnostics.Monitoring.TestCommon.Options
#else
namespace Microsoft.Diagnostics.Tools.Monitor.Auth.ApiKey
#endif
{
    internal static class JwtAlgorithmChecker
    {
        /// <summary>
        /// This is the list of allowed algorithms for JWS that are public/private key based.
        /// We also reject RSASSA-PSS because .net core does not have support for this algorithm.
        /// </summary>
        /// <remarks>
        /// We enforce this list to prevent storage of private key information in configuration.
        /// Pulled from: https://datatracker.ietf.org/doc/html/rfc7518#section-3.1
        /// </remarks>
        private static readonly string[] AllowedJwtAlgos = new string[]
        {
            // ECDSA using curves P-X and SHA-X
            SecurityAlgorithms.EcdsaSha256, SecurityAlgorithms.EcdsaSha256Signature,
            SecurityAlgorithms.EcdsaSha384, SecurityAlgorithms.EcdsaSha384Signature,
            SecurityAlgorithms.EcdsaSha512, SecurityAlgorithms.EcdsaSha512Signature,

            // RSASSA-PKCS1-v1_5 using SHA-x
            SecurityAlgorithms.RsaSha256, SecurityAlgorithms.RsaSha256Signature,
            SecurityAlgorithms.RsaSha384, SecurityAlgorithms.RsaSha384Signature,
            SecurityAlgorithms.RsaSha512, SecurityAlgorithms.RsaSha512Signature,
        };

        /// <summary>
        /// This is the list of JSON Web Key Key Types that we support. Specifically these are the values that are 
        /// valid for the 'kty' field.
        /// </summary>
        /// <remarks>
        /// Again we allow all the algorithms that are public/private to prevent private key information in configuration.
        /// Pulled from: https://datatracker.ietf.org/doc/html/rfc7518#section-7.4.2
        /// </remarks>
        private static readonly string[] AllowedJwkKeyTypes = new string[]
        {
            // Elliptic curve
            JsonWebAlgorithmsKeyTypes.EllipticCurve, 
            // RSA
            JsonWebAlgorithmsKeyTypes.RSA
        };

        public static IReadOnlyList<string> GetAllowedJwsAlgorithmList()
        {
            return new List<string>(AllowedJwtAlgos);
        }

        public static bool IsValidJwk(JsonWebKey key)
        {
            return
                !string.IsNullOrEmpty(key.Kty)
                && AllowedJwkKeyTypes.Contains(key.Kty, StringComparer.OrdinalIgnoreCase);
        }
    }
}
