// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    /// <summary>
    /// Service that monitors API Key authentication options changes and logs issues with the specified options.
    /// </summary>
    internal class ApiKeyAuthenticationOptionsObserver :
        IDisposable
    {
        private readonly ILogger<ApiKeyAuthenticationOptionsObserver> _logger;
        private readonly IOptionsMonitor<ApiKeyAuthenticationOptions> _options;

        private IDisposable _changeRegistration;

        public ApiKeyAuthenticationOptionsObserver(
            ILogger<ApiKeyAuthenticationOptionsObserver> logger,
            IOptionsMonitor<ApiKeyAuthenticationOptions> options
            )
        {
            _logger = logger;
            _options = options;
        }

        public void Initialize()
        {
            _changeRegistration = _options.OnChange(OnApiKeyAuthenticationOptionsChanged);

            // Write out current validation state of options when starting the tool.
            CheckApiKeyAuthenticationOptions(_options.CurrentValue);
        }

        public void Dispose()
        {
            _changeRegistration?.Dispose();
        }

        private void OnApiKeyAuthenticationOptionsChanged(ApiKeyAuthenticationOptions options)
        {
            _logger.ApiKeyAuthenticationOptionsChanged();

            CheckApiKeyAuthenticationOptions(options);
        }

        private void CheckApiKeyAuthenticationOptions(ApiKeyAuthenticationOptions options)
        {
            // ValidationErrors will be null if API key authentication is not enabled.
            if (null != options.ValidationErrors && options.ValidationErrors.Any())
            {
                _logger.ApiKeyValidationFailures(options.ValidationErrors);
            }
        }
    }
}
