// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class AuthenticationStartupLogger :
        IStartupLogger
    {
        private readonly IAuthConfiguration _authConfiguration;
        private readonly AddressListenResults _listenResults;
        private readonly ILogger _logger;
        private readonly MonitorApiKeyConfigurationObserver _observer;

        public AuthenticationStartupLogger(
            IAuthConfiguration authConfiguration,
            AddressListenResults listenResults,
            MonitorApiKeyConfigurationObserver observer,
            ILogger<Startup> logger)
        {
            _authConfiguration = authConfiguration;
            _listenResults = listenResults;
            _logger = logger;
            _observer = observer;
        }

        public void Log()
        {
            if (_authConfiguration.KeyAuthenticationMode == KeyAuthenticationMode.NoAuth)
            {
                _logger.NoAuthentication();
            }
            else
            {
                if (_authConfiguration.KeyAuthenticationMode == KeyAuthenticationMode.TemporaryKey)
                {
                    _logger.LogTempKey(_authConfiguration.TemporaryJwtKey.Token);
                }

                //Auth is enabled and we are binding on http. Make sure we log a warning.
                if (_listenResults.HasInsecureAuthentication)
                {
                    _logger.InsecureAuthenticationConfiguration();
                }
            }

            _observer.Initialize();
        }
    }
}
