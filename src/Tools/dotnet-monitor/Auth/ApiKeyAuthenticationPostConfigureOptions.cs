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
                if (!HashAlgorithmChecker.IsAllowedAlgorithm(sourceOptions.ApiKeyHashType))
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
