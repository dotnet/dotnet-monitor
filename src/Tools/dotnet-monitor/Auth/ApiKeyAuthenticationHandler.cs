﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using System;
using System.Buffers.Text;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    /// <summary>
    /// Authenticates against the ApiKey stored on the server.
    /// </summary>
    internal sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
    {
        public const int ApiKeyByteMinLength = 32;
        public const int ApiKeyByteMaxLength = 2048;
        // This is the max length to efficiently encode an ApiKey of the length ApiKeyByteMaxLength.
        private readonly int ApiKeyBase64MaxLength = Base64.GetMaxEncodedToUtf8Length(ApiKeyByteMaxLength);

        public ApiKeyAuthenticationHandler(
            IOptionsMonitor<ApiKeyAuthenticationOptions> options,
            ILoggerFactory loggerFactory,
            UrlEncoder encoder,
            ISystemClock clock)
            : base(options, loggerFactory, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            ApiKeyAuthenticationOptions options = OptionsMonitor.CurrentValue;
            if (options.ValidationErrors.Any())
            {
                Logger.ApiKeyValidationFailures(options.ValidationErrors);

                return Task.FromResult(AuthenticateResult.Fail(Strings.ErrorMessage_ApiKeyNotConfigured));
            }

            //We are expecting a header such as Authorization: <Schema> <key>
            //If this is not present, we will return NoResult and move on to the next authentication handler.
            if (!Request.Headers.TryGetValue(HeaderNames.Authorization, out StringValues values) ||
                !values.Any())
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            if (!AuthenticationHeaderValue.TryParse(values.First(), out AuthenticationHeaderValue authHeader))
            {
                return Task.FromResult(AuthenticateResult.Fail(Strings.ErrorMessage_InvalidAuthHeader));
            }

            if (!string.Equals(authHeader.Scheme, Scheme.Name, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            if (authHeader.Parameter == null)
            {
                return Task.FromResult(AuthenticateResult.Fail(Strings.ErrorMessage_InvalidApiKeyFormat));
            }

            if (authHeader.Parameter.Length > ApiKeyBase64MaxLength)
            {
                return Task.FromResult(AuthenticateResult.Fail(Strings.ErrorMessage_InvalidApiKeyFormat));
            }

            // Calculate the max length of the base64 encoded value,
            // the rules to decode this are really complex and allow white space in the
            // string which should be ignored, simply calculate the max it can be.
            int byteLen = Base64.GetMaxDecodedFromUtf8Length(authHeader.Parameter.Length);

            if (byteLen < ApiKeyByteMinLength)
            {
                return Task.FromResult(AuthenticateResult.Fail(Strings.ErrorMessage_InvalidApiKeyFormat));
            }

            // The user is passing a base 64-encoded version of the secret
            // We will be hash this and compare it to the secret in our configuration.
            byte[] buffer = new byte[byteLen];
            Span<byte> span = new Span<byte>(buffer);
            if (!Convert.TryFromBase64String(authHeader.Parameter, span, out int bytesWritten) || bytesWritten < ApiKeyByteMinLength || bytesWritten > ApiKeyByteMaxLength)
            {
                return Task.FromResult(AuthenticateResult.Fail(Strings.ErrorMessage_InvalidApiKeyFormat));
            }

            Debug.Assert(null != options.HashAlgorithm);
            using HashAlgorithm algorithm = HashAlgorithm.Create(options.HashAlgorithm);
            Debug.Assert(null != algorithm);

            byte[] hashedSecret = algorithm.ComputeHash(buffer, 0, bytesWritten);

            Debug.Assert(null != options.HashValue);
            if (hashedSecret.SequenceEqual(options.HashValue))
            {
                return Task.FromResult(AuthenticateResult.Success(
                    new AuthenticationTicket(
                        new ClaimsPrincipal(new[] { new ClaimsIdentity(AuthConstants.ApiKeySchema) }),
                        AuthConstants.ApiKeySchema)));
            }
            else
            {
                return Task.FromResult(AuthenticateResult.Fail(Strings.ErrorMessage_InvalidApiKey));
            }
        }
    }
}
