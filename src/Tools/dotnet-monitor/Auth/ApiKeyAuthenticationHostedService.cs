// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    /// <summary>
    /// Service that monitors API Key authentication options changes and logs issues with the specified options.
    /// </summary>
    internal class ApiKeyAuthenticationHostedService :
        IHostedService,
        IDisposable
    {
        private readonly ILogger<ApiKeyAuthenticationHostedService> _logger;
        private readonly IOptionsMonitor<ApiKeyAuthenticationOptions> _options;

        private IDisposable _changeRegistration;

        public ApiKeyAuthenticationHostedService(
            ILogger<ApiKeyAuthenticationHostedService> logger,
            IOptionsMonitor<ApiKeyAuthenticationOptions> options
            )
        {
            _logger = logger;
            _options = options;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _changeRegistration = _options.OnChange(OnApiKeyAuthenticationOptionsChanged);

            // Write out current validation state of options when starting the tool.
            CheckApiKeyAuthenticationOptions(_options.CurrentValue);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _changeRegistration.Dispose();

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _changeRegistration.Dispose();
        }

        private void OnApiKeyAuthenticationOptionsChanged(ApiKeyAuthenticationOptions options)
        {
            _logger.ApiKeyAuthenticationOptionsChanged();

            CheckApiKeyAuthenticationOptions(options);
        }

        private void CheckApiKeyAuthenticationOptions(ApiKeyAuthenticationOptions options)
        {
            if (options.ValidationErrors.Any())
            {
                _logger.ApiKeyValidationFailures(options.ValidationErrors);
            }
        }
    }
}
