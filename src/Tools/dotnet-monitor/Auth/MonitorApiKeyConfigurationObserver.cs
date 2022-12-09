// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    /// <summary>
    /// Service that monitors API Key authentication options changes and logs issues with the specified options.
    /// </summary>
    internal class MonitorApiKeyConfigurationObserver :
        IDisposable
    {
        private readonly ILogger<MonitorApiKeyConfigurationObserver> _logger;
        private readonly IOptionsMonitor<MonitorApiKeyConfiguration> _options;
        private readonly IAuthConfiguration _authConfigurationOptions;

        private IDisposable _changeRegistration;

        public MonitorApiKeyConfigurationObserver(
            ILogger<MonitorApiKeyConfigurationObserver> logger,
            IOptionsMonitor<MonitorApiKeyConfiguration> options,
            IAuthConfiguration authConfigurationOptions)
        {
            _logger = logger;
            _options = options;
            _authConfigurationOptions = authConfigurationOptions;
        }

        public void Initialize()
        {
            if (_authConfigurationOptions.KeyAuthenticationMode != KeyAuthenticationMode.NoAuth)
            {
                _changeRegistration = _options.OnChange(OnMonitorApiKeyOptionsChanged);

                // Write out current validation state of options when starting the tool.
                CheckMonitorApiKeyOptions(_options.CurrentValue);
            }
        }

        public void Dispose()
        {
            _changeRegistration?.Dispose();
        }

        private void OnMonitorApiKeyOptionsChanged(MonitorApiKeyConfiguration options)
        {
            CheckMonitorApiKeyOptions(options);
        }

        private void CheckMonitorApiKeyOptions(MonitorApiKeyConfiguration options)
        {
            if (options.Configured)
            {
                if (null != options.ValidationErrors && options.ValidationErrors.Any())
                {
                    _logger.ApiKeyValidationFailures(options.ValidationErrors);
                }
                else
                {
                    _logger.ApiKeyAuthenticationOptionsValidated();
                }
            }
            else
            {
                _logger.MonitorApiKeyNotConfigured();
            }
        }
    }
}
