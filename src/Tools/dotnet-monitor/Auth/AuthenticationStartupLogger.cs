﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Tools.Monitor.Auth;
using Microsoft.Extensions.Logging;
using System;

namespace Microsoft.Diagnostics.Tools.Monitor
{
    internal sealed class AuthenticationStartupLogger :
        IStartupLogger
    {
        private readonly AddressListenResults _listenResults;
        private readonly ILogger _logger;
        private readonly IAuthenticationConfigurator _authConfigurator;
        private readonly IServiceProvider _serviceProvider;

        public AuthenticationStartupLogger(
            IServiceProvider serviceProvider,
            AddressListenResults listenResults,
            IAuthenticationConfigurator authConfigurator,
            ILogger<Startup> logger)
        {
            _listenResults = listenResults;
            _logger = logger;
            _authConfigurator = authConfigurator;
            _serviceProvider = serviceProvider;
        }

        public void Log()
        {
            _authConfigurator.LogStartup(_logger, _serviceProvider);

            // Auth is enabled and we are binding on http. Make sure we log a warning.
            // (HasInsecureAuthentication will only be true if we're using https and not using NoAuth)
            if (_listenResults.HasInsecureAuthentication)
            {
                _logger.InsecureAuthenticationConfiguration();
            }
        }
    }
}
