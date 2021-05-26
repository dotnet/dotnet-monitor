// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    /// <summary>
    /// Sets options on ApiKeyAuthenticationOptions based on ApiAuthenticationOptions.
    /// </summary>
    internal sealed class ApiKeyAuthenticationPostConfigureOptions :
        IPostConfigureOptions<ApiKeyAuthenticationOptions>
    {
        private static readonly string[] DisallowedHashAlgorithms = new string[]
        {
            // ------------------   SHA1    ------------------
            "SHA",
            "SHA1",
            "System.Security.Cryptography.SHA1",
            "System.Security.Cryptography.SHA1Cng",
            "System.Security.Cryptography.HashAlgorithm",
            "http://www.w3.org/2000/09/xmldsig#sha1",
            // These give a KeyedHashAlgorith based on SHA1
            "System.Security.Cryptography.HMAC",
            "System.Security.Cryptography.KeyedHashAlgorithm",
            "HMACSHA1",
            "System.Security.Cryptography.HMACSHA1",
            "http://www.w3.org/2000/09/xmldsig#hmac-sha1",
            
            // ------------------    MD5    ------------------
            "MD5",
            "System.Security.Cryptography.MD5",
            "System.Security.Cryptography.MD5Cng",
            "http://www.w3.org/2001/04/xmldsig-more#md5",
            // These give a KeyedHashAlgorith based on MD5
            "HMACMD5",
            "System.Security.Cryptography.HMACMD5",
            "http://www.w3.org/2001/04/xmldsig-more#hmac-md5",
            
            // These are defined in .net framework but currently not supported
            // supported in .net core. Lets add these to the list for future 
            // proofing, in the event that support is expanded.
            // See: https://github.com/dotnet/runtime/blob/01b7e73cd378145264a7cb7a09365b41ed42b240/src/libraries/System.Security.Cryptography.Algorithms/src/System/Security/Cryptography/CryptoConfig.cs#L275
            // ------------------ RIPEMD160 ------------------
            "RIPEMD160",
            "RIPEMD-160",
            "System.Security.Cryptography.RIPEMD160",
            "System.Security.Cryptography.RIPEMD160Managed",
            "http://www.w3.org/2001/04/xmlenc#ripemd160",
            // These give a KeyedHashAlgorith based on RIPEMD160
            "HMACRIPEMD160",
            "System.Security.Cryptography.HMACRIPEMD160",
            "http://www.w3.org/2001/04/xmldsig-more#hmac-ripemd160",
            
            // ------------------  MAC3DES  ------------------
            // This is .net specific non-crypto hash algorithm, don't allow it
            "MACTripleDES",
            "System.Security.Cryptography.MACTripleDES",
        };

        private readonly IOptionsMonitor<ApiAuthenticationOptions> _apiAuthOptions;

        public ApiKeyAuthenticationPostConfigureOptions(
            IOptionsMonitor<ApiAuthenticationOptions> apiAuthOptions)
        {
            _apiAuthOptions = apiAuthOptions;
        }

        public void PostConfigure(string name, ApiKeyAuthenticationOptions options)
        {
            ApiAuthenticationOptions sourceOptions = _apiAuthOptions.CurrentValue;

            IList<ValidationResult> errors = new List<ValidationResult>();

            Validator.TryValidateObject(
                sourceOptions,
                new ValidationContext(sourceOptions, null, null),
                errors,
                validateAllProperties: true);

            // Validate hash algorithm is allowed and is supported.
            if (!string.IsNullOrEmpty(sourceOptions.ApiKeyHashType))
            {
                if (DisallowedHashAlgorithms.Contains(sourceOptions.ApiKeyHashType, StringComparer.OrdinalIgnoreCase))
                {
                    errors.Add(new ValidationResult($"The {nameof(ApiAuthenticationOptions.ApiKeyHashType)} field value '{sourceOptions.ApiKeyHashType}' is not allowed.", new string[] { nameof(ApiAuthenticationOptions.ApiKeyHashType) }));
                }
                else
                {
                    using HashAlgorithm algorithm = HashAlgorithm.Create(sourceOptions.ApiKeyHashType);
                    if (null == algorithm)
                    {
                        errors.Add(new ValidationResult($"The {nameof(ApiAuthenticationOptions.ApiKeyHashType)} field value '{sourceOptions.ApiKeyHashType}' is not supported.", new string[] { nameof(ApiAuthenticationOptions.ApiKeyHashType) }));
                    }
                }
            }

            byte[] apiKeyHashBytes = null;
            if (!string.IsNullOrEmpty(sourceOptions.ApiKeyHash))
            {
                // ApiKeyHash is represented as a hex string. e.g. AABBCCDDEEFF
                if (sourceOptions.ApiKeyHash.Length % 2 == 0)
                {
                    apiKeyHashBytes = new byte[sourceOptions.ApiKeyHash.Length / 2];
                    for (int i = 0; i < sourceOptions.ApiKeyHash.Length; i += 2)
                    {
                        if (!byte.TryParse(sourceOptions.ApiKeyHash.AsSpan(i, 2), NumberStyles.HexNumber, provider: NumberFormatInfo.InvariantInfo, result: out byte resultByte))
                        {
                            errors.Add(new ValidationResult($"The {nameof(ApiAuthenticationOptions.ApiKeyHash)} field could not be decoded as hex string.", new string[] { nameof(ApiAuthenticationOptions.ApiKeyHash) }));
                            break;
                        }
                        apiKeyHashBytes[i / 2] = resultByte;
                    }
                }
                else
                {
                    errors.Add(new ValidationResult($"The {nameof(ApiAuthenticationOptions.ApiKeyHash)} field value length must be an even number.", new string[] { nameof(ApiAuthenticationOptions.ApiKeyHash) }));
                }
            }

            options.ValidationErrors = errors;
            if (errors.Any())
            {
                options.HashAlgorithm = null;
                options.HashValue = null;
            }
            else
            {
                options.HashAlgorithm = sourceOptions.ApiKeyHashType;
                options.HashValue = apiKeyHashBytes;
            }
        }
    }
}
