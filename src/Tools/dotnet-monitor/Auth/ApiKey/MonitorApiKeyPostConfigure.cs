// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Validation;
using Microsoft.Diagnostics.Monitoring.WebApi;
using Microsoft.Diagnostics.Tools.Monitor.CollectionRules.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;

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
        private readonly IServiceProvider _serviceProvider;
        private readonly ValidationOptions _validationOptions;

        public MonitorApiKeyPostConfigure(
            IServiceProvider serviceProvider,
            ILogger<MonitorApiKeyPostConfigure> logger,
            IOptionsMonitor<MonitorApiKeyOptions> apiKeyOptions)
        {
            _logger = logger;
            _apiKeyOptions = apiKeyOptions;
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _validationOptions = serviceProvider.GetRequiredService<IOptions<ValidationOptions>>().Value;
        }

        public void PostConfigure(string? name, MonitorApiKeyConfiguration options)
        {
            MonitorApiKeyOptions sourceOptions = _apiKeyOptions.CurrentValue;

            List<ValidationResult> errors = new List<ValidationResult>();


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

            ValidationHelper.TryValidateObject(sourceOptions, typeof(MonitorApiKeyOptions), _validationOptions, errors);

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
