// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Microsoft.Diagnostics.Tools.Monitor.Auth.ApiKey
{
    /// <summary>
    /// Sets options on <see cref="MonitorApiKeyConfiguration"/> based on <see cref="MonitorApiKeyOptions"/>.
    /// This class is responsible for decoding the value provided in <see cref="MonitorApiKeyOptions.PublicKey"/> and validating it.
    /// </summary>
    internal sealed class MonitorApiKeyPostConfigure :
        IPostConfigureOptions<MonitorApiKeyConfiguration>
    {
        private readonly ILogger<MonitorApiKeyPostConfigure> _logger;
        private readonly IOptionsMonitor<MonitorApiKeyOptions> _apiKeyOptions;

        public MonitorApiKeyPostConfigure(
            ILogger<MonitorApiKeyPostConfigure> logger,
            IOptionsMonitor<MonitorApiKeyOptions> apiKeyOptions)
        {
            _logger = logger;
            _apiKeyOptions = apiKeyOptions;
        }

        public void PostConfigure(string? name, MonitorApiKeyConfiguration options)
        {
            MonitorApiKeyOptions sourceOptions = _apiKeyOptions.CurrentValue;

            IList<ValidationResult> errors = new List<ValidationResult>();

            // If nothing is set, lets not attach an error and instead pass along the blank config
            if (sourceOptions.Subject == null && sourceOptions.PublicKey == null)
            {
                options.Configured = false;
                options.Subject = null;
                options.PublicKey = null;
                return;
            }

            // Some options are configured (but may not be valid)
            options.Configured = true;

            Validator.TryValidateObject(
                sourceOptions,
                new ValidationContext(sourceOptions, null, null),
                errors,
                validateAllProperties: true);

            string? jwkJson = null;
            try
            {
                jwkJson = Base64UrlEncoder.Decode(sourceOptions.PublicKey);
            }
            catch (Exception)
            {
                errors.Add(
                    new ValidationResult(
                        string.Format(
                            Strings.ErrorMessage_NotBase64,
                            nameof(MonitorApiKeyOptions.PublicKey),
                            sourceOptions.PublicKey),
                        new string[] { nameof(MonitorApiKeyOptions.PublicKey) }));
            }

            JsonWebKey? jwk = null;
            if (!string.IsNullOrEmpty(jwkJson))
            {
                try
                {
                    jwk = JsonWebKey.Create(jwkJson);
                }
                // JsonWebKey will throw only throw ArgumentException or a derived class.
                catch (ArgumentException ex)
                {
                    errors.Add(
                        new ValidationResult(
                            string.Format(
                                Strings.ErrorMessage_InvalidJwk,
                                nameof(MonitorApiKeyOptions.PublicKey),
                                sourceOptions.PublicKey,
                                ex.Message),
                            new string[] { nameof(MonitorApiKeyOptions.PublicKey) }));
                }
            }

            if (null != jwk)
            {
                if (!JwtAlgorithmChecker.IsValidJwk(jwk))
                {
                    errors.Add(
                        new ValidationResult(
                            string.Format(
                                Strings.ErrorMessage_RejectedJwk,
                                nameof(MonitorApiKeyOptions.PublicKey)),
                            new string[] { nameof(MonitorApiKeyOptions.PublicKey) }));
                }
                // We will let the algorithm work with private key but we should produce a warning message
                else if (jwk.HasPrivateKey)
                {
                    _logger.NotifyPrivateKey(nameof(MonitorApiKeyOptions.PublicKey));
                }
            }

            options.ValidationErrors = errors;
            if (errors.Any())
            {
                options.Subject = string.Empty;
                options.PublicKey = null;
                options.Issuer = string.Empty;
            }
            else
            {
                options.Subject = sourceOptions.Subject;
                options.PublicKey = jwk;
                options.Issuer = string.IsNullOrEmpty(sourceOptions.Issuer) ?
                    AuthConstants.ApiKeyJwtInternalIssuer :
                    sourceOptions.Issuer;
            }
        }
    }
}
